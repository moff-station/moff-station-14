using Content.Shared._Moffstation.Overlay.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.ArInterface.Components;

/// <summary>
/// Mark an entity as being part of an augmented reality setting.
/// </summary>
[RegisterComponent]
public sealed partial class ArElementComponent : Component
{
    /// <summary>
    /// The augmented reality interface this entity is part of.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ArInterfacePrototype> Interface;
}
