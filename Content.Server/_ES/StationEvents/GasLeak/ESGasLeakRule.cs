using Content.Server._ES.StationEvents.GasLeak.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.StationEvents.GasLeak;

public sealed partial class ESGasLeakRule : StationEventSystem<ESGasLeakRuleComponent>
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESGasLeakRuleComponent, ESSynchronizedVotesCompletedEvent>(OnSynchronizedVotesCompleted);
        SubscribeLocalEvent<ESGasVentVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
    }

    private void OnSynchronizedVotesCompleted(Entity<ESGasLeakRuleComponent> ent, ref ESSynchronizedVotesCompletedEvent args)
    {
        if (!args.TryGetResult<ESGasVoteOption>(0, out var gasResult) ||
            !args.TryGetResult<ESEntityVoteOption>(1, out var ventResult) ||
            !TryGetEntity(ventResult.Entity, out var vent))
        {
            ForceEndSelf((ent.Owner, null));
            return;
        }

        ent.Comp.LeakGas = gasResult.Gas;
        ent.Comp.LeakOrigin = vent.Value;
    }

    private void OnGetVoteOptions(Entity<ESGasVentVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        if (!_station.TryGetRandomStation<StationEventEligibleComponent>(out var station))
            return;
        var ventList = new List<Entity<TransformComponent>>();
        var query = EntityQueryEnumerator<GasVentPumpComponent, TransformComponent>();
        while (query.MoveNext(out var ventUid, out _, out var xform))
        {
            if (_station.GetOwningStation(ventUid, xform) == station.Value.Owner)
                ventList.Add((ventUid, xform));
        }

        var count = Math.Min(ent.Comp.Count, ventList.Count);
        for (var i = 0; i < count; i++)
        {
            var vent = RobustRandom.PickAndTake(ventList);
            var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(vent.AsNullable()));
            args.Options.Add(new ESEntityVoteOption
            {
                DisplayString = location,
                Entity = GetNetEntity(vent),
            });
        }
    }

    protected override void Started(Entity<ESGasLeakRuleComponent, GameRuleComponent> rule, ref GameRuleStartedEvent args)
    {
        base.Started(rule, ref args);

        rule.Comp1.NextLeakTime = Timing.CurTime;
        rule.Comp1.LeakRate = RobustRandom.NextFloat(rule.Comp1.LeakRateRange.X, rule.Comp1.LeakRateRange.Y);
        var moles = RobustRandom.NextFloat(rule.Comp1.LeakMolesRange.X, rule.Comp1.LeakMolesRange.Y);
        Comp<StationEventComponent>(rule).EndTime = Timing.CurTime + TimeSpan.FromSeconds(moles / rule.Comp1.LeakRate) + TimeSpan.FromSeconds(1);
    }

    protected override void ActiveTick(EntityUid uid, ESGasLeakRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!Exists(component.LeakOrigin) || TerminatingOrDeleted(component.LeakOrigin))
            return;

        if (Timing.CurTime < component.NextLeakTime)
            return;
        component.NextLeakTime += component.LeakDelay;

        var mixture = _atmosphere.GetTileMixture(component.LeakOrigin);
        mixture?.AdjustMoles(component.LeakGas, (float) (component.LeakRate * component.LeakDelay).TotalSeconds);
    }

    protected override void Ended(Entity<ESGasLeakRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);

        if (TerminatingOrDeleted(rule)) // Moff - Rules ended by entity deletion skip end effects
            return;

        if (!RobustRandom.Prob(rule.Comp.SparkChance))
            return;

        if (!Exists(rule.Comp.LeakOrigin) || TerminatingOrDeleted(rule.Comp.LeakOrigin))
            return;

        if (Transform(rule.Comp.LeakOrigin).GridUid is not { } grid)
            return;

        var indices = _transform.GetGridTilePositionOrDefault(rule.Comp.LeakOrigin);
        _atmosphere.HotspotExpose(grid, indices, 700f, 50f, null, true);
        Audio.PlayPvs(rule.Comp.SparkSound, rule);
    }
}
