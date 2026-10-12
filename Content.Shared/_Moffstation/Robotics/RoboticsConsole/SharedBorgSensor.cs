using Content.Shared._Moffstation.Sensors;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Robotics;
using Content.Shared.StatusIcon;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Robotics.RoboticsConsole;

[Serializable, NetSerializable]
public readonly record struct BorgSensorStatus(SuitSensorStatus Status, CyborgControlData Control, TimeSpan TimeOut)
{
    public readonly TimeSpan Timestamp = Status.Timestamp;
    public readonly ProtoId<SensorTypePrototype> SensorType = Status.SensorType;
    public readonly NetEntity SuitSensorUid = Status.SuitSensorUid;
    public readonly NetEntity OwnerUid = Status.OwnerUid;
    public readonly NetCoordinates? Coordinates = Status.Coordinates;
    public readonly string Name = Status.Name;
    public readonly ProtoId<JobIconPrototype> JobIcon = Status.JobIcon;
    public readonly bool IsAlive = Status.IsAlive;
    public readonly int? TotalDamage = Status.TotalDamage;
    public readonly int? TotalDamageThreshold = Status.TotalDamageThreshold;
    public readonly float HpPercent = Control.HpPercent;

    public readonly SpriteSpecifier? ChassisSprite = Control.ChassisSprite;
    public readonly string ChassisName = Control.ChassisName;
    public readonly float Charge = Control.Charge;
    public readonly int ModuleCount = Control.ModuleCount;
    public readonly bool HasBrain = Control.HasBrain;
    public readonly bool CanDisable = Control.CanDisable;
    public readonly TimeSpan Timeout = TimeOut;

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
    public readonly NetEntity Target = target;
}
