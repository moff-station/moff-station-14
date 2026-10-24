using Content.Shared._Moffstation.Sensors;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

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
