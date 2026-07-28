using Content.Shared._Moffstation.Medical.CrewMonitoring;
using Content.Shared._Moffstation.Robotics.RoboticsConsole;
using Content.Shared.Robotics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Robotics.RoboticsConsole;

[UsedImplicitly]
public sealed class MoffRoboticsConsoleBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private MoffRoboticsConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<MoffRoboticsConsoleWindow>();
        _window.OnDisablePressed += OnDisablePressed;
        _window.OnDestroyPressed += OnDestroyPressed;

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

        _window.Setup(gridUid, stationName);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not MoffRoboticsConsoleState cast)
            return;

        _window?.Update(cast);
    }

    private void OnDisablePressed(NetEntity borg)
    {
        SendMessage(new MoffRoboticsConsoleDisableMessage(borg));
    }

    private void OnDestroyPressed(NetEntity borg)
    {
        SendMessage(new MoffRoboticsConsoleDestroyMessage(borg));
    }
}

