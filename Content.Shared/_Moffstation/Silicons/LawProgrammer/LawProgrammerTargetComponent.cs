namespace Content.Shared._Moffstation.Silicons.LawProgrammer;

/// <summary>
/// This is used for entity that react to contact with a LawReprogrammer component.
/// </summary>

[RegisterComponent]
public sealed partial class LawProgrammerTargetComponent : Component
{
    /// <summary>
    /// Indicate if the entity is immune to reprogramming attempts.
    /// </summary>
    [DataField]
    public bool IsImmune = false;

    /// <summary>
    /// Multiplicative factor of the doAfter duration when attempting to configure this entity
    /// </summary>
    [DataField]
    public float DurationMultiplier = 1f;
}
