
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
public sealed partial class EvacArrivalsSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ArrivalsSystem _arrivals = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ShuttleSystem _shuttle = default!;
    [Dependency] private StationSystem _station = default!;

    [Dependency] private EntityQuery<EvacArrivalsComponent> _evacArrivalsQuery;
    [Dependency] private EntityQuery<ShuttleComponent> _shuttleQuery;

    private static readonly ProtoId<TagPrototype> DockTag = "DockEmergency";
    private static readonly LocId CallBlockedReason = "evac-arrival-call-blocked";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in EntityQueryEnumerator<EvacArrivalsComponent, ShuttleComponent>())
        {
            if (ent.Comp1.DepartTime is not { } depart || depart > _timing.CurTime)
                continue;

            if (HasComp<FTLComponent>(ent))
                continue;

            ent.Comp1.State = EvacArrivalsState.Returning;
            ent.Comp1.DepartTime = null;
            _shuttle.FTLToCoordinates(ent, ent.Comp2, ent.Comp1.Origin, ent.Comp1.OriginRotation);
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

            var xform = Transform(shuttle);
            var arrival = EnsureComp<EvacArrivalsComponent>(shuttle);
            arrival.Station = station;
            arrival.Origin = xform.Coordinates;
            arrival.OriginRotation = xform.LocalRotation;
            shuttleComp.FTLCooldownOverride = TimeSpan.Zero;

            _shuttle.FTLToDock(shuttle,
                shuttleComp,
                target,
                startupTime: 0f,
                hyperspaceTime: _cfg.GetCVar(MoffCCVars.EvacArrivalFTLTime),
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

        foreach (var _ in EntityQueryEnumerator<EvacArrivalsComponent>())
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
            && arrival.State != EvacArrivalsState.Returning)
            args.Grid = shuttle;
    }

    [SubscribeLocalEvent]
    private void OnEvacDepartureCheck(Entity<EvacArrivalsComponent> ent, ref EmergencyShuttleEvacDepartureCheckEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnFTLStarted(Entity<EvacArrivalsComponent> ent, ref FTLStartedEvent args)
    {
        switch (ent.Comp.State)
        {
            case EvacArrivalsState.InTransit:

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
            case EvacArrivalsState.Returning:
                _arrivals.DumpChildren(ent, ref args);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnFTLCompleted(Entity<EvacArrivalsComponent> ent, ref FTLCompletedEvent args)
    {
        switch (ent.Comp.State)
        {
            case EvacArrivalsState.InTransit:
                var dockTime = TimeSpan.FromSeconds(_cfg.GetCVar(MoffCCVars.EvacArrivalDockTime));
                ent.Comp.State = EvacArrivalsState.Docked;
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
            case EvacArrivalsState.Returning:
                if (_shuttleQuery.HasComp(ent))
                    RemCompDeferred<EvacArrivalsComponent>(ent);
                break;
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
