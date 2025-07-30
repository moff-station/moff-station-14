using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.Medical.CrewMonitoring;

namespace Content.Server._Moffstation.Medical.CrewMonitoring;

/// <summary>
/// This handles...
/// </summary>
public sealed class LongRangeCrewMonitorSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LongRangeCrewMonitorComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnUIOpened(Entity<LongRangeCrewMonitorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!TryComp<StationDataComponent>(_station.GetStationInMap(Transform(ent.Owner).MapID),
                out var stationDataComponent))
            return;

        ent.Comp.TargetGrid = _station.GetLargestGrid(stationDataComponent);
        Dirty(ent);
    }
}
