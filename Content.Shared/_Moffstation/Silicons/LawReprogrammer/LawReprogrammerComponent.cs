namespace Content.Shared._Moffstation.Silicons.LawReprogrammer;

/// <summary>
/// This is used for items that can change silicon laws on contact
/// </summary>
[RegisterComponent]
public sealed partial class LawReprogrammerComponent : Component
{
    /// <summary>
    /// Minimum delay between consecutive uses.
    /// </summary>
    [DataField]
    public TimeSpan DelayBetweenUses = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Entity containing the laws that will be uploaded to the target on uses.
    /// </summary>
    public EntityUid? LawSource;

    public TimeSpan NextAllowedUsed = TimeSpan.Zero;
}
