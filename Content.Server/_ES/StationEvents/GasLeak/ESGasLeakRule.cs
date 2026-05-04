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
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ES.StationEvents.GasLeak;

public sealed class ESGasLeakRule : StationEventSystem<ESGasLeakRuleComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

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
            ForceEndSelf(ent);
            return;
        }

        ent.Comp.LeakGas = gasResult.Gas;
        ent.Comp.LeakOrigin = vent.Value;
    }

    private void OnGetVoteOptions(Entity<ESGasVentVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        if (!TryGetRandomStation(out var station))
            return;
        var ventList = new List<Entity<TransformComponent>>();
        var query = EntityQueryEnumerator<GasVentPumpComponent, TransformComponent>();
        while (query.MoveNext(out var ventUid, out _, out var xform))
        {
            if (_station.GetOwningStation(ventUid, xform) == station)
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

    protected override void Started(EntityUid uid, ESGasLeakRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextLeakTime = Timing.CurTime;
        component.LeakRate = RobustRandom.NextFloat(component.LeakRateRange.X, component.LeakRateRange.Y);
        var moles = RobustRandom.NextFloat(component.LeakMolesRange.X, component.LeakMolesRange.Y);
        Comp<StationEventComponent>(uid).EndTime = Timing.CurTime + TimeSpan.FromSeconds(moles / component.LeakRate) + TimeSpan.FromSeconds(1);
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

    protected override void Ended(EntityUid uid, ESGasLeakRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!RobustRandom.Prob(component.SparkChance))
            return;

        if (!Exists(component.LeakOrigin) || TerminatingOrDeleted(component.LeakOrigin))
            return;

        if (Transform(component.LeakOrigin).GridUid is not { } grid)
            return;

        var indices = _transform.GetGridTilePositionOrDefault(component.LeakOrigin);
        _atmosphere.HotspotExpose(grid, indices, 700f, 50f, null, true);
        Audio.PlayPvs(component.SparkSound, uid);
    }
}
