using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Traits.Assorted;

/// <summary>
/// This is used for the uncloneable trait.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UncloneableComponent : Component;
