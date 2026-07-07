namespace Content.Shared._Moffstation.Silicons.Borgs;

/// <summary>
/// This is used for entities that have a dedicated slot containing an item with an <see cref="IdCardComponent"/>
/// and an <see cref="SuitSensorComponent"/>, allowing them to be seen via <see cref="CrewMonitorComponent"/> without
/// any piece of equipment.
/// </summary>
[RegisterComponent]
public sealed partial class SensorContainerComponent : Component
{
    public const string SensorContainerId = "sensor-container";

    [DataField]
    public string ContainerId = SensorContainerId;

    [DataField]
    public EntityUid? SensorEntity;
}
