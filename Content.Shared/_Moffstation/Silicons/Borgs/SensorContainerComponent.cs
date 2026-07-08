using Content.Shared.Access.Components;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Prototypes;

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

    /// <summary>
    /// ID of the container holding the sensor entity
    /// </summary>
    [DataField]
    public string ContainerId = SensorContainerId;

    /// <summary>
    /// Prototype of the entity with the <see cref="SuitSensorComponent"/> and <see cref="IdCardComponent"/>
    /// </summary>
    /// <remarks>
    /// Ideally would be done via a ContainerFillComponent but this would be overriden to all hell in many cases.
    /// </remarks>
    [DataField(required: true)]
    public EntProtoId Sensor;

    [DataField]
    public EntityUid? SensorEntity;
}
