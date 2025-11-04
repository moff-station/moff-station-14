using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Traits.Assorted;

/// <summary>
/// This is used for the uncloneable trait.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UncloneableComponent : Component
{
    /// <summary>
    /// Does a health analyzer say if this person can be cloned?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Analyzable = true;
}
