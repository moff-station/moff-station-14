using Content.Shared.Robotics;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Robotics.RoboticsConsole;

[Serializable, NetSerializable]
public sealed class MoffRoboticsConsoleState(List<BorgSensorStatus> cyborgs, bool allowBorgControl)
    : BoundUserInterfaceState
{
    /// <summary>
    /// Map of device network addresses to cyborg data.
    /// </summary>
    public List<BorgSensorStatus> Cyborgs = cyborgs;

    /// <summary>
    /// If the UI will have the buttons to disable and destroy.
    /// </summary>
    public bool AllowBorgControl = allowBorgControl;
}
