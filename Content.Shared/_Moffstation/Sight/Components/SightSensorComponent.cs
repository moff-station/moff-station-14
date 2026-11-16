namespace Content.Shared._Moffstation.Sight.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SightSensorComponent : Component
{
    /// <summary>
    /// The range at which the sensor is able to see a target.
    /// </summary>
    [DataField(required: true)]
    public string FixtureId;

    /// <summary>
    /// If true the sensor is able to see through walls
    /// </summary>
    [DataField]
    public bool IgnoreOcclusion;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public string Key = "";
}

/// <summary>
/// Raised on an entity when entering the sightline of a sensor
/// </summary>
/// <param name="sensor"></param>
[ByRefEvent]
public record struct EnterSightlineEvent(EntityUid target);

/// <summary>
/// Raised on an entity when leaving the sightline of a sensor
/// </summary>
/// <param name="sensor"></param>
[ByRefEvent]
public record struct ExitSightlineEvent(EntityUid target);
