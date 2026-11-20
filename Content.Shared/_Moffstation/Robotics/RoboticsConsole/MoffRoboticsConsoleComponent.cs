using Content.Shared._Moffstation.Sensors;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Radio;
using Content.Shared.Robotics;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Robotics.RoboticsConsole;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MoffRoboticsConsoleComponent : Component
{
    /// <summary>
    /// The type of sensors that will appear on this console.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<SensorTypePrototype>> SensorTypes = new();

    /// <summary>
    /// Can borgs be destroyed or deactivated from this console.
    /// </summary>
    [DataField]
    public bool AllowBorgControl = false;

    /// <summary>
    /// Address and data of each cyborg.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, BorgSensorStatus> Cyborgs = new();

    /// <summary>
    /// After not sending data for this lenght of time the borg data are removed from the console.
    /// </summary>
    [DataField]
    public TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Radio channel to send messages on when a cyborg is destroyed.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Science";

    /// <summary>
    /// Message sent on the radio channel when a cyborg is destroyed.
    /// </summary>
    [DataField]
    public LocId DestroyMessage = "robotics-console-cyborg-destroying";

    /// <summary>
    /// Cooldown on destroying borgs to prevent complete abuse.
    /// </summary>
    [DataField]
    public TimeSpan DestroyCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When a borg can next be destroyed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDestroy = TimeSpan.Zero;
}



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

/// <summary>
/// Raised when the button to destroy a borg is pressed
/// </summary>
/// <param name="target"></param>
[Serializable, NetSerializable]
public sealed class MoffRoboticsConsoleDestroyMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

/// <summary>
/// Raised when the button to disable a borg is pressed
/// </summary>
/// <param name="target"></param>
[Serializable, NetSerializable]
public sealed class MoffRoboticsConsoleDisableMessage(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

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
