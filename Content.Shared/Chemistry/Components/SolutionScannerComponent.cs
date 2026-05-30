using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Allows an entity to examine reagents inside of containers, puddles and similiar via the examine verb.
/// Works when added either directly to an entity or to piece of clothing worn by that entity.
/// </summary>
// Moffstation - Begin - Solution scanners can be toggled
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SolutionScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
// Moffstation - End
