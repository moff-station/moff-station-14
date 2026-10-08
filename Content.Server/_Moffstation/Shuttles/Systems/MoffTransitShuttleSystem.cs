
using System.Numerics;
using Content.Server._Moffstation.Shuttles.Components;
using Content.Server._Moffstation.Spawners;
using Content.Server.Communications;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking.Events;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.CCVar;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Shuttles.Systems;

/// Makes the evac shuttle make a trip to the station roundstart, intended to be used like the arrivals shuttle.
/// I named it "transit" because I think being naming it something like "EvacArrivals" is far more confusing
public sealed partial class MoffTransitShuttleSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ArrivalsSystem _arrivals = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ShuttleSystem _shuttle = default!;
    [Dependency] private StationSystem _station = default!;

    [Dependency] private EntityQuery<MoffTransitShuttleComponent> _evacArrivalsQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] private EntityQuery<ShuttleComponent> _shuttleQuery;
    [Dependency] private EntityQuery<StationCentcommComponent> _centcommQuery;

    private static readonly ProtoId<TagPrototype> DockTag = "DockEmergency";
    private static readonly LocId CallBlockedReason = "evac-arrival-call-blocked";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in EntityQueryEnumerator<MoffTransitShuttleComponent, ShuttleComponent>())
        {
            if (ent.Comp1.DepartTime is not { } depart || depart > _timing.CurTime)
                continue;

            if (HasComp<FTLComponent>(ent))
                continue;

            ent.Comp1.State = TransitShuttleState.Returning;
            ent.Comp1.DepartTime = null;

            if (_centcommQuery.TryComp(ent.Comp1.Station, out var centcomm) && Exists(centcomm.Entity))
            {
                _shuttle.FTLToDock(ent, ent.Comp2, centcomm.Entity.Value);
                continue;
            }

            var map = _map.CreateMap();
            _shuttle.FTLToCoordinates(ent, ent.Comp2, new EntityCoordinates(map, Vector2.Zero), Angle.Zero);
        }
    }

    [SubscribeLocalEvent]
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        if (!_cfg.GetCVar(MoffCCVars.StartAtArrivals) || !_cfg.GetCVar(CCVars.EmergencyShuttleEnabled))
            return;

        foreach (var station in EntityQueryEnumerator<StationEmergencyShuttleComponent>())
        {
            if (station.Comp.EmergencyShuttle is not { } shuttle ||
                !_shuttleQuery.TryComp(shuttle, out var shuttleComp) ||
                _station.GetLargestGrid(station.Owner) is not { } target)
                continue;

            var arrival = EnsureComp<MoffTransitShuttleComponent>(shuttle);
            arrival.Station = station;
            shuttleComp.FTLCooldownOverride = TimeSpan.Zero;

            _shuttle.FTLToDock(shuttle,
                shuttleComp,
                target,
                startupTime: 0f,
                hyperspaceTime: _cfg.GetCVar(MoffCCVars.TransitArrivalFTLTime),
                priorityTag: DockTag);

            // So people don't get knocked on their ass when they spawn
            if (TryComp<FTLComponent>(shuttle, out var ftl))
                ftl.KnockdownOnStart = false;
        }
    }

    [SubscribeLocalEvent]
    private void OnCallShuttleAttempt(ref CommunicationConsoleCallShuttleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var _ in EntityQueryEnumerator<MoffTransitShuttleComponent>())
        {
            args.Cancelled = true;
            args.Reason = Loc.GetString(CallBlockedReason);
            return;
        }
    }

    [SubscribeLocalEvent]
    private void OnGetArrivalsSpawnGrid(Entity<StationEmergencyShuttleComponent> ent, ref GetArrivalsSpawnGridEvent args)
    {
        if (ent.Comp.EmergencyShuttle is { } shuttle
            && _evacArrivalsQuery.TryComp(shuttle, out var arrival)
            && arrival.State != TransitShuttleState.Returning)
            args.Grid = shuttle;
    }

    [SubscribeLocalEvent]
    private void OnEvacDepartureCheck(Entity<MoffTransitShuttleComponent> ent, ref EvacShuttleDepartureCheckEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnFTLStarted(Entity<MoffTransitShuttleComponent> ent, ref FTLStartedEvent args)
    {
        switch (ent.Comp.State)
        {
            case TransitShuttleState.InTransit:

                if (!TryComp<FTLComponent>(ent, out var ftl))
                    break;

                ftl.KnockdownOnStart = true;

                var eta = TimeSpan.FromSeconds(ftl.TravelTime);
                var payload = new NetworkPayload
                {
                    [ShuttleTimerMasks.ShuttleMap] = ent.Owner,
                    [ShuttleTimerMasks.ShuttleTime] = eta,
                    [ShuttleTimerMasks.DestMap] = _transform.GetMap(args.TargetCoordinates),
                    [ShuttleTimerMasks.DestTime] = eta,
                    [ScreenMasks.Text] = ShuttleTimerMasks.ETA,
                };

                if (TryComp<DeviceNetworkComponent>(ent.Owner, out var net))
                    _deviceNetwork.QueuePacket(ent.Owner, null, payload, net.TransmitFrequency);
                break;
            case TransitShuttleState.Returning:
                SendCrewToStation(ent);
                _arrivals.DumpChildren(ent, ref args);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnFTLCompleted(Entity<MoffTransitShuttleComponent> ent, ref FTLCompletedEvent args)
    {
        switch (ent.Comp.State)
        {
            case TransitShuttleState.InTransit:
                var dockTime = TimeSpan.FromSeconds(_cfg.GetCVar(MoffCCVars.TransitArrivalDockTime));
                ent.Comp.State = TransitShuttleState.Docked;
                ent.Comp.DepartTime = _timing.CurTime + dockTime;
                RefillStationBatteries(ent.Comp.Station);

                var payload = new NetworkPayload
                {
                    [ShuttleTimerMasks.ShuttleMap] = ent.Owner,
                    [ShuttleTimerMasks.ShuttleTime] = dockTime,
                    [ShuttleTimerMasks.SourceMap] = args.MapUid,
                    [ShuttleTimerMasks.SourceTime] = dockTime,
                    [ShuttleTimerMasks.Docked] = true,
                    [ScreenMasks.Text] = ShuttleTimerMasks.ETD,
                };
                if (TryComp<DeviceNetworkComponent>(ent.Owner, out var net))
                    _deviceNetwork.QueuePacket(ent.Owner, null, payload, net.TransmitFrequency);
                break;
            case TransitShuttleState.Returning:
                if (_shuttleQuery.HasComp(ent))
                    RemCompDeferred<MoffTransitShuttleComponent>(ent);
                break;
        }
    }

    // Anyone who stays on evac when it leaves gets shipped to the station
    private void SendCrewToStation(Entity<MoffTransitShuttleComponent> ent)
    {
        foreach (var mind in EntityQueryEnumerator<MindComponent>())
        {
            if (mind.Comp.OwnedEntity is { } body
                && _mobStateQuery.HasComp(body)
                && Transform(body).GridUid == ent.Owner)
                _arrivals.TryTeleportToMapSpawn(body, ent.Comp.Station);
        }
    }

    private void RefillStationBatteries(EntityUid station)
    {
        foreach (var battery in EntityQueryEnumerator<BatteryRefillOnArrivalComponent, BatteryComponent>())
        {
            if (_station.GetOwningStation(battery) == station)
                _battery.SetCharge((battery.Owner, battery.Comp2), battery.Comp2.MaxCharge);
        }
    }
}
