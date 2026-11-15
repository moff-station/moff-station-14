using Content.Shared._Moffstation.Overlay.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.ArInterface.Components;

/// <summary>
/// Used on entities who have elements associated to them in some augmented reality interface.
/// </summary>
[RegisterComponent]
public sealed partial class ArDisplaysComponent : Component
{
    /// <summary>
    /// prototypes associated to this entity by interface.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ArInterfacePrototype>, List<EntProtoId>> Displays = [];

    /// <summary>
    /// entities currently spawned.
    /// </summary>
    [DataField, NonSerialized]
    public Dictionary<ProtoId<ArInterfacePrototype>, List<EntityUid>> Entities = [];
}


/// <summary>
/// Raised directed on entity CLIENT-SIDE ONLY to spawn / despawn entities
/// </summary>
/// <param name="Interfaces">Active virtual reality interfaces</param>
[ByRefEvent]
public record struct ArDisplayUpdateEvent(HashSet<ProtoId<ArInterfacePrototype>> Interfaces);
