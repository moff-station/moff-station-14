using Content.Shared._Moffstation.Overlay.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.ArInterface.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArDisplaysComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<ArInterfacePrototype>, List<EntProtoId>> Displays;

    [DataField, NonSerialized]
    public Dictionary<ProtoId<ArInterfacePrototype>, List<EntityUid>> EntDisplays = [];
}

/// <summary>
/// Event raised directed on an entity CLIENT-SIDE ONLY
/// in order to get what entities to display in the augmented reality interface.
/// </summary>
/// <param name="Interfaces">active interfaces</param>
/// <param name="Displays">hashset of entities to be displayed</param>
[ByRefEvent]
public record struct GetArDisplaysEvent(in List<ProtoId<ArInterfacePrototype>> Interfaces, HashSet<EntityUid> Displays);
