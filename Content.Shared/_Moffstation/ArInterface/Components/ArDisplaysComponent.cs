using Content.Shared._Moffstation.Overlay.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.ArInterface.Components;

/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class ArDisplaysComponent : Component
{
    /// <summary>
    /// What prototype will be spawned for different augmented reality interfaces
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ArInterfacePrototype>, List<ArDisplayEntry>> Displays;
}

/// <summary>
/// Event raised directed on an entity CLIENT-SIDE ONLY
/// to update the entities
/// </summary>
/// <param name="Interfaces"></param>
[ByRefEvent]
public record struct UpdateArDisplayEvent(HashSet<ProtoId<ArInterfacePrototype>> Interfaces);

/// <summary>
/// Event raised directed on an entity CLIENT-SIDE ONLY
/// in order to get what entities to display in the augmented reality interface.
/// </summary>
/// <param name="Interfaces">active interfaces</param>
/// <param name="Displays">hashset of entities to be displayed</param>
[ByRefEvent]
public record struct GetArDisplaysEvent(in List<ProtoId<ArInterfacePrototype>> Interfaces, HashSet<EntityUid> Displays);

/// <summary>
/// Associate a prototype to a spawned instance of this prototype.
/// </summary>
[DataDefinition]
public partial record struct ArDisplayEntry
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    public EntityUid? Entity;

    public ArDisplayEntry(EntProtoId prototype)
    {
        Prototype = prototype;
        Entity = null;
    }
}
