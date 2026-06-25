using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server._Moffstation.Power.Components;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.Light.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Station.Components;
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

    protected override void Started(EntityUid entity, LightOverloadRuleComponent component, GameRuleComponent ruleData, GameRuleStartedEvent args)
    {
        base.Started(entity, component, ruleData, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var stationApcs = new List<Entity<ApcComponent, TransformComponent>>();
        var query = EntityQueryEnumerator<ApcComponent, TransformComponent>();
        while (query.MoveNext(out var apcUid, out var apc, out var xform))
        {
            if (apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == chosenStation)
            {
                stationApcs.Add((apcUid, apc, xform));
            }
        }

        var selectedApc = stationApcs[_random.Next(stationApcs.Count)];
        _apcSystem.ApcToggleBreaker(selectedApc);
        var apcCoords = selectedApc.Comp2.Coordinates;
        Spawn(component.SparksPrototype, apcCoords);

        var message = Loc.GetString("light-overload-announcement",
            ("location", FormattedMessage.RemoveMarkupOrThrow(
                _navMap.GetNearestBeaconString((selectedApc, selectedApc.Comp2)))));

        _chatSystem.DispatchStationAnnouncement(chosenStation.Value, message, playDefaultSound: true, colorOverride: Color.Yellow);

        foreach (var light in _entityLookup.GetEntitiesInRange<PoweredLightComponent>(
                     apcCoords,
                     component.Radius))
        {
            if (_random.Prob(component.LightOverloadProbability))
            {
                var explodeTimer = EnsureComp<LightExplodeTimerComponent>(light);
                explodeTimer.ExplodeTimer = _random.NextFloat((float) component.MaxDelay.TotalSeconds);
                explodeTimer.SparksPrototype = component.SparksPrototype;
                explodeTimer.SparksProbability = component.SparksProbability;
            }

            if (_random.Prob(component.LightBlinkingProbability))
            {
                var blinking = EnsureComp<BlinkingPoweredLightComponent>(light);
                blinking.StopBlinkingTime = _gameTime.CurTime + component.BlinkTime;
            }
        }
    }
}
