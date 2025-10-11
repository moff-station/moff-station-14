namespace Content.Server._Moffstation.RandomItem;

[RegisterComponent]
public sealed partial class RandomItemComponent : Component
{
    /// <summary>
    /// Restricts spawning to items in the whitelist
    /// </summary>
    [DataField]
    public string[] Whitelist = default!;

    /// <summary>
    /// Prevents blacklisted things from spawning
    /// </summary>
    [DataField]
    public string[] Blacklist = default!;

    /// <summary>
    /// Whether or not the spawn should be limited only to actual items.
    /// </summary>
    [DataField]
    public bool InsaneMode;
}
