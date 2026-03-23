using Content.Client.Medical.CrewMonitoring;
using Content.Shared._Moffstation.Medical.CrewMonitoring;
using Content.Shared._Moffstation.Silicons.StationAi;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;
using YamlDotNet.Core.Tokens;

namespace Content.Client._Moffstation.Silicons.StationAi;

public sealed class StationAiMonitorBoundUserInterface : BoundUserInterface
{
    [Dependency] IEntityManager _entityManager = default!;
    private SharedStationAiMonitorSystem _aiMonitorSystem;

    [ViewVariables]
    private StationAiMonitorWindow? _window;

    public StationAiMonitorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _aiMonitorSystem = _entityManager.System<SharedStationAiMonitorSystem>();
    }

    protected override void Open()
    {
        base.Open();

        EntityUid? gridUid = null;
        var stationName = String.Empty;

        if (!_entityManager.TryGetNetEntity(Owner, out var netEnt))
            return;


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

        _window= this.CreateWindow<StationAiMonitorWindow>();
        _window.SetStation(stationName, gridUid);


        _window.GotoClicked += coords =>
        {
            if (netEnt is { } ent)
                _aiMonitorSystem.RequestStationAiWarp(ent, coords);
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case CrewMonitoringState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                _window?.ShowSensors(st.Sensors);
                break;
        }
    }
}
