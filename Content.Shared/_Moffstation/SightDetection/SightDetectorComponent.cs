namespace Content.Shared._Moffstation.SightDetection;

[RegisterComponent]
public sealed partial class SightDetectorComponent : Component
{
    /// <summary>
    /// Maximum range
    /// </summary>
    [DataField]
    public float Range;

    /// <summary>
    /// Can the entity sight items through opaque objects
    /// </summary>
    [DataField]
    public bool IgnoreOpacity;

    /// <summary>
    /// Entities that were sighted previously
    /// </summary>
    [DataField, NonSerialized]
    public HashSet<EntityUid> Sights = new HashSet<EntityUid>();

    /// <summary>
    /// Time between consecutive updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateCooldown;

    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

/// <summary>
/// Raised on the detector entity when a target have been sighted
/// </summary>
/// <param name="sighted"></param>
[ByRefEvent]
public record struct DetectorSeeTargetEvent(EntityUid sighted);

/// <summary>
/// Raised on the detector entity when a target is no longer in sight
/// </summary>
/// <param name="sighted"></param>
[ByRefEvent]
public record struct DetectorUnseeTargetEvent(EntityUid sighted);
