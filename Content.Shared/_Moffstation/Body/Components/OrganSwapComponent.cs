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
    /// A mapping of <c>slot name : organ prototype</c> for the organs to replace in those particular containers/slot.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> OrganSwaps = new();

    public override bool SendOnlyToOwner => true;
}

