namespace Content.Shared._Moffstation.SightDetection;

/// <summary>
///     Make the entity able to detect when a <see cref="SightDetectorTargetComponent"/> enter or exist its line of sight.
/// </summary>
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
    /// The detector only detect targets with the corresponding key.
    /// </summary>
    [DataField(required:true)]
    public string Key = default!;

    /// <summary>
    /// Entities that were sighted previously
    /// </summary>
    [DataField, NonSerialized]
    public HashSet<EntityUid> Sights = [];

    /// <summary>
    /// Time between consecutive updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(0.25);

    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

/// <summary>
/// Raised on the detector entity when a target have been sighted
/// </summary>
/// <param name="sighted"></param>
[ByRefEvent]
public record struct DetectorSeeTargetEvent(EntityUid Target);

/// <summary>
/// Raised on the detector entity when a target is no longer in sight
/// </summary>
/// <param name="sighted"></param>
[ByRefEvent]
public record struct DetectorUnseeTargetEvent(EntityUid Target);
