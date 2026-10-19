using Content.Shared._Moffstation.Sensors;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.Silicons.Borgs;

/// <summary>
/// This is used to allow a borg to appear in crew monitoring consoles accepting the sensor type indicated
/// in the component <see cref="SensorTypePrototype"/>.
/// </summary>
[AutoGenerateComponentState, AutoGenerateComponentPause]
[RegisterComponent, NetworkedComponent]
public sealed partial class BorgSensorComponent : Component
{
    /// <summary>
    ///     Choose a random sensor mode when spawned.
    /// </summary>
    [DataField]
    public bool RandomMode = true;

    /// <summary>
    ///     If true user can't change suit sensor mode
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ControlsLocked = false;

    /// <summary>
    ///     How much time it takes to change another player's sensors
    /// </summary>
    [DataField]
    public float SensorsTime = 1.75f;

    /// <summary>
    ///     Current sensor mode. Can be switched by user verbs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SuitSensorMode Mode = SuitSensorMode.SensorOff;

    /// <summary>
    ///     How often does sensor update its owners status (in seconds). Limited by the system update rate.
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(2f);

    /// <summary>
    ///     Next time when sensor updated owners status
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    ///     The server the suit sensor sends it state to.
    ///     The suit sensor will try connecting to a new server when no server is connected.
    ///     It does this by calling the servers entity system for performance reasons.
    /// </summary>
    [DataField("server")]
    public string? ConnectedServer = null;

    /// <summary>
    ///     The station this suit sensor belongs to. If it's null the suit didn't spawn on a station and the sensor doesn't work.
    /// </summary>
    [DataField("station"), AutoNetworkedField]
    public EntityUid? StationId = null;


    /// <summary>
    ///     The type of sensor sent to the server <see cref="SensorTypePrototype"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SensorTypePrototype> SensorType = new("NTBorgSensor");
}
