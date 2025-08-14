using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.Medical.CrewMonitoring;
using Content.Shared.Station.Components;

namespace Content.Server._Moffstation.Medical.CrewMonitoring;

public sealed class LongRangeCrewMonitorSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LongRangeCrewMonitorComponent, ComponentInit>(UpdateTargetGrid);
        SubscribeLocalEvent<LongRangeCrewMonitorComponent, BoundUIOpenedEvent>(UpdateTargetGrid);
    }

    private void UpdateTargetGrid<T>(Entity<LongRangeCrewMonitorComponent> ent, ref T args)
    {
        var station = _station.GetStationInMap(Transform(ent.Owner).MapID);
        if (!HasComp<StationDataComponent>(station))
            return;

        ent.Comp.TargetGrid = _station.GetLargestGrid(station.Value);
        Dirty(ent);
    }
}
