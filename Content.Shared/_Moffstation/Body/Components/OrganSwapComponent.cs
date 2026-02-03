using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Body.Components;

/// <summary>
/// A component that on initialization swaps out the organs within a body.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganSwapComponent : Component
{
    /// <summary>
    /// A mapping of <seealso cref="OrganCategoryPrototype"/> to organ prototype which specifies which category of organ
    /// to swap to what new organ.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntProtoId> OrganSwaps = new();

    public override bool SendOnlyToOwner => true;
}

