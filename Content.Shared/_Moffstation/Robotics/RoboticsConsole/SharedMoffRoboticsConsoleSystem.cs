using Content.Shared.Robotics;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Robotics.RoboticsConsole;

public abstract class SharedMoffRoboticsConsoleSystem : EntitySystem
{
}


[Serializable, NetSerializable]
public sealed class MoffRoboticsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// Map of device network addresses to cyborg data.
    /// </summary>
    public List<BorgSensorStatus> Cyborgs;

    /// <summary>
    /// If the UI will have the buttons to disable and destroy.
    /// </summary>
    public bool AllowBorgControl;

    public MoffRoboticsConsoleState(List<BorgSensorStatus> cyborgs, bool allowBorgControl)
    {
        Cyborgs = cyborgs;
        AllowBorgControl = allowBorgControl;
    }
}
