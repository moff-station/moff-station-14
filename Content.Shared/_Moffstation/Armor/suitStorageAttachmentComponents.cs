using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Armor;

/// Allow an entity with <see cref="SuitStorageAttachableComponent"/> to be attached to this entity.
[RegisterComponent, NetworkedComponent]
public sealed partial class SuitStorageAttachableComponent : Component
{
    [DataField]
    public string AttachmentSlotId = "suit-storage-attachment";

    /// <see cref="SuitStorageAttachmentComponent"/>s attached to this component are stored in this slot while attached.
    [ViewVariables]
    public ContainerSlot Slot = default!;

    /// This factor is applied to <see cref="SuitStorageAttachmentComponent.AttachDelay"/> and
    /// <see cref="SuitStorageAttachmentComponent.DetachDelay"/> to determine the duration of the respective doafters.
    [DataField]
    public float AttachDelayModifier = 1.0f;

    [DataField]
    public LocId CanAttachText = "attachablesuitstorage-attachable-can-attach";

    [DataField]
    public LocId HasAttachmentText = "attachablesuitstorage-attachable-has-attachment";

    [DataField]
    public LocId AttachVerbName = "attachablesuitstorage-attachable-verb-attach";

    [DataField]
    public SpriteSpecifier? AttachIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/insert.svg.192dpi.png"));

    [DataField]
    public LocId DetachVerbName = "attachablesuitstorage-attachable-verb-detach";

    [DataField]
    public SpriteSpecifier? DetachIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"));
}

/// Can be attached to entities with <see cref="SuitStorageAttachableComponent"/> to enable storage of entities which
/// pass <see cref="Whitelist"/> in suitstorage while the attachable entity is worn.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SuitStorageAttachmentComponent : Component
{
    /// Whitelist for what entities are allowed in the suit storage slot.
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new() { Components = ["Item"] };

    /// How long it takes to attach this attachment. Modified by <see cref="SuitStorageAttachableComponent.AttachDelayModifier"/>.
    [DataField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(2);

    /// How long it takes to detach this attachment. Modified by <see cref="SuitStorageAttachableComponent.AttachDelayModifier"/>.
    [DataField]
    public TimeSpan DetachDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public LocId CanAttachText = "attachablesuitstorage-attachment-can-be-attached";
}

/// Event raised on the entity with <see cref="SuitStorageAttachableComponent"/> when an entity with
/// <see cref="SuitStorageAttachmentComponent"/> is attached to it.
[Serializable, NetSerializable]
public sealed partial class SuitStorageAttachmentAttachEvent : SimpleDoAfterEvent;

/// Event raised on the entity with <see cref="SuitStorageAttachableComponent"/> when an entity with
/// <see cref="SuitStorageAttachmentComponent"/> is detached from it.
[Serializable, NetSerializable]
public sealed partial class SuitStorageAttachmentDetachEvent : SimpleDoAfterEvent;
