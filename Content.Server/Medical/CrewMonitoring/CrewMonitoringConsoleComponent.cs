using Content.Shared._Moffstation.Sensors; // Moffstation - Borg sensors
using Content.Shared.DeviceNetwork;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility; // Moffstation - Borg sensors

namespace Content.Server.Medical.CrewMonitoring;

/// <summary>
/// Stores suit sensor status received by a crew monitoring console.
/// </summary>
[RegisterComponent]
[Access(typeof(CrewMonitoringConsoleSystem))]
public sealed partial class CrewMonitoringConsoleComponent : Component
{
    /// <summary>
    /// List of all currently connected sensors to this console.
    /// </summary>
    public Dictionary<DeviceAddress, SuitSensorStatus> ConnectedSensors = new();

    /// <summary>
    /// How long without an update before a sensor is considered lost, in seconds.
    /// </summary>
    [DataField]
    public float SensorTimeout = 10f;

    // Moffstation - Begin - Borg sensors
    /// <summary>
    ///     Types of sensor datas accepted by this crew monitor
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SensorTypePrototype>> SensorTypes = new();
    // Moffstation - End
}
