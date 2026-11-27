using Content.Shared._Moffstation.Overlay.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Overlay.Components;

/// <summary>
///     Allow you to see an augmented reality interface.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowArInterfaceComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ArInterfacePrototype> Interface;
}
