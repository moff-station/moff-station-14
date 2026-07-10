using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Sensors;

/// <summary>
/// This is a prototype defining a type of sensor.
/// Used by <see cref="CrewMonitorConsoleComponent"/> to select the sensors to display.
/// </summary>
[Prototype()]
public sealed partial class SensorTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;
}
