using Content.Shared._Moffstation.Sensors; // Moffstation - Borg sensors
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Prototypes; // Moffstation - Borg sensors

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    ///     List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<string, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    // Moffstation - Begin - Borg sensors
    /// <summary>
    ///     Types of sensor datas accepted by this crew monitor
    /// </summary>
    [DataField]
    public PrototypeFlags<SensorTypePrototype> SensorTypes = new();
    // Moffstation - End
}
