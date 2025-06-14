using Content.Shared.Body.Organ;
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
    /// A mapping of <c>container name : organ prototype</c> for the organs to replace in those particular containers/slot.
    /// </summary>
    [DataField]
    public Dictionary<string, string> OrganSwaps = new();
}
