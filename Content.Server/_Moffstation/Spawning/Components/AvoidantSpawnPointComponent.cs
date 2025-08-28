using Content.Shared.Whitelist;

namespace Content.Server._Moffstation.Spawning.Components;

/// <summary>
/// Attached to a spawn point which avoids nearby players when spawning.
/// </summary>
[RegisterComponent]
public sealed partial class AvoidantSpawnPointComponent : Component
{
    /// <summary>
    /// The distance within which to check for any entities.
    /// </summary>
    [DataField]
    public float Range = 10f;

    /// <summary>
    /// A blacklist of entities to avoid.
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new();
}
