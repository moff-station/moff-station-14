using Content.Shared._Moffstation.Sensors;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Robotics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Robotics.RoboticsConsole;

[Serializable, NetSerializable]
public sealed class BorgSensorStatus(SuitSensorStatus status, CyborgControlData control)
{
    public TimeSpan Timestamp = status.Timestamp;
    public ProtoId<SensorTypePrototype> SensorType = status.SensorType;
    public NetEntity SuitSensorUid = status.SuitSensorUid;
    public NetEntity OwnerUid = status.OwnerUid;
    public NetCoordinates? Coordinates = status.Coordinates;
    public string Name = status.Name;
    public string JobIcon = status.JobIcon;
    public bool IsAlive = status.IsAlive;
    public int? TotalDamage = status.TotalDamage;
    public int? TotalDamageThreshold = status.TotalDamageThreshold;

    public SpriteSpecifier? ChassisSprite = control.ChassisSprite;
    public string ChassisName = control.ChassisName;
    public float Charge = control.Charge;
    public int ModuleCount = control.ModuleCount;
    public bool HasBrain = control.HasBrain;
    public bool CanDisable = control.CanDisable;
    public TimeSpan Timeout = TimeSpan.Zero;

    public float? DamagePercentage => TotalDamageThreshold == null || TotalDamage == null ? null : TotalDamage / (float) TotalDamageThreshold;
}

[Serializable, NetSerializable]
public sealed class MoffRoboticsConsoleDestroyMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class MoffRoboticsConsoleDisableMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target;
}
