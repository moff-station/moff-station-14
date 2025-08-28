using Content.Shared.Whitelist;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

/// <summary>
/// Attached to a spawn point which avoids nearby players when spawning.
/// </summary>
[RegisterComponent]
public sealed partial class AvoidantSpawningComponent : Component
{
    /// <summary>
    /// The distance within which to check for any entities.
    /// </summary>
    [DataField]
    public float Range = 3f;

    /// <summary>
    /// A blacklist of entities to avoid.
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new();
}
