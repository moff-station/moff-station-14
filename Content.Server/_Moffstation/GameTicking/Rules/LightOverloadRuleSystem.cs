using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server._Moffstation.Power.Components;
using Content.Server.Chat.Systems;
using Content.Server.Light.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Light.Components;
using Content.Shared.Station.Components;
using Content.Shared.Station.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.GameTicking.Rules;

/// <summary>
/// This rule selects a random APC on the station and breaks lights connected to it, with accompanying sparks and sounds.
/// </summary>
public sealed partial class LightOverloadRuleSystem : GameRuleSystem<LightOverloadRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ApcSystem _apcSystem = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private IGameTiming _gameTime = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private ChatSystem _chatSystem = default!;
    [Dependency] private StationSystem _station = default!;

    protected override void Started(Entity<LightOverloadRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);
        var overloadComp = rule.Comp1;

        if (!_station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenStation))
            return;

        var stationApcs = new List<Entity<ApcComponent, TransformComponent>>();
        var query = EntityQueryEnumerator<ApcComponent, TransformComponent>();
        while (query.MoveNext(out var apcUid, out var apc, out var xform))
        {
            if (apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == chosenStation.Value.Owner)
            {
                stationApcs.Add((apcUid, apc, xform));
            }
        }

        var selectedApc = stationApcs[_random.Next(stationApcs.Count)];
        _apcSystem.ApcToggleBreaker(selectedApc);
        var apcCoords = selectedApc.Comp2.Coordinates;
        Spawn(overloadComp.SparksPrototype, apcCoords);

        var message = Loc.GetString(overloadComp.Announcement,
            ("location", FormattedMessage.RemoveMarkupOrThrow(
                _navMap.GetNearestBeaconString((selectedApc, selectedApc.Comp2)))));

        _chatSystem.DispatchStationAnnouncement(chosenStation.Value, message, playDefaultSound: true, colorOverride: overloadComp.AnnouncementColor);

        foreach (var light in _entityLookup.GetEntitiesInRange<PoweredLightComponent>(
                     apcCoords,
                     overloadComp.Radius))
        {
            if (_random.Prob(overloadComp.LightOverloadProbability))
            {
                var explodeTimer = EnsureComp<LightExplodeTimerComponent>(light);
                explodeTimer.ExplodeTimer = _random.NextFloat((float) overloadComp.MaxDelay.TotalSeconds);
                explodeTimer.SparksPrototype = overloadComp.SparksPrototype;
                explodeTimer.SparksProbability = overloadComp.SparksProbability;
            }

            // We have both of these probabilities get rolled on all the lights so
            // sometimes there is a light that starts blinking then explodes.
            // For the flavor.
            if (_random.Prob(overloadComp.LightBlinkingProbability))
            {
                var blinking = EnsureComp<BlinkingPoweredLightComponent>(light);
                blinking.StopBlinkingTime = _gameTime.CurTime + overloadComp.BlinkTime;
            }
        }
    }
}
