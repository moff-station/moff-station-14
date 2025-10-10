using Content.Shared._Moffstation.Medical.CrewMonitoring;   // Moffstation
using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;

namespace Content.Client.Medical.CrewMonitoring;

public sealed class CrewMonitoringBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CrewMonitoringWindow? _menu;

    public CrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        EntityUid? gridUid = null;
        var stationName = string.Empty;

        // Moffstation - Long range monitor implementation
        if (EntMan.TryGetComponent<LongRangeCrewMonitorComponent>(Owner, out var longRangeComp))
        {
            gridUid = longRangeComp.TargetGrid;
        }
        else if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }
        if (EntMan.TryGetComponent<MetaDataComponent>(gridUid, out var metaData))
        {
            stationName = metaData.EntityName;
        }
        // Moffstation - End

        _menu = this.CreateWindow<CrewMonitoringWindow>();
        _menu.Set(stationName, gridUid);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CrewMonitoringState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                _menu?.ShowSensors(st.Sensors, Owner, xform?.Coordinates);
                break;
        }
    }
}
