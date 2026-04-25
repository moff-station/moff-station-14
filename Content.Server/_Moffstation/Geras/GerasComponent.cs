using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Geras;

/// <summary>
/// This component assigns the entity with a transformation action, and stores the entity to transform into.
/// </summary>
[RegisterComponent]
public sealed partial class GerasComponent : Component
{
    /// <summary>
    /// The prototype of the mob to transform into
    /// </summary>
    [DataField] public EntProtoId? GerasProto;

    [DataField] public EntProtoId GerasAction = "ActionMorphGeras";

    [DataField] public EntityUid? GerasActionEntity;

    public EntityUid StorageMap;

    /// <summary>
    /// The entity that the entity will transform into on transforming.
    /// </summary>
    [DataField]
    public EntityUid? Geras;

    public bool VisualsLoaded;
}
