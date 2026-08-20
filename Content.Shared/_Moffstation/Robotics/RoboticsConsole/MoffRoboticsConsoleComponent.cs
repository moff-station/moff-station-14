using Content.Shared._Moffstation.Sensors;
using Content.Shared.Radio;
using Content.Shared.Robotics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.Robotics.RoboticsConsole;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]

public sealed partial class MoffRoboticsConsoleComponent : Component
{
    /// <summary>
    /// Sensor types detected by this console.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<SensorTypePrototype>> SensorTypes;

    /// <summary>
    /// Can borgs be destroyed or deactivated from this console
    /// </summary>
    [DataField]
    public bool AllowBorgControl;

    /// <summary>
    /// Address and data of each cyborg.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, BorgSensorStatus> Cyborgs = new();

    /// <summary>
    /// After not responding for this length of time borgs are removed from the console.
    /// </summary>
    [DataField]
    public TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Radio channel to send messages on.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Science";

    /// <summary>
    /// Radio message sent when destroying a borg.
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
