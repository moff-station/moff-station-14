using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// Marks an entity as allowed to use the necronomicon.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BibleSystem))]
public sealed partial class NecronomiconUserComponent : Component
{
    public override bool SendOnlyToOwner => true;
}
