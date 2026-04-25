using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CryomachineComponent : Component
{
    #region Slots

    /// <summary>
    /// The ID of the itemslot that holds the cryocapsule
    /// </summary>
    [DataField]
    public string CapsuleSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that holds the cryocapsule.
    /// </summary>
    [DataField]
    public ItemSlot CapsuleSlot = new();

    /// <summary>
    /// The ID of the itemslot that holds beakers
    /// </summary>
    [DataField]
    public string BeakerSlotId = "beaker_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that can hold beakers.
    /// </summary>
    [DataField]
    public ItemSlot BeakerSlot = new();

    #endregion

    /// <summary>
    /// The sound emitted when we shock the brain.
    /// </summary>
    [DataField]
    public SoundSpecifier ShockSound = new SoundPathSpecifier("/Audio/Machines/airlock_electrify_on.ogg");

    [DataField]
    public SoundSpecifier DetachSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}

[Serializable, NetSerializable]
public enum CryomachineUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CryomachineSimpleUiMessage : BoundUserInterfaceMessage
{
    public enum MessageType { JumpstartBrain, DetachCapsule, EjectBeaker }

    public readonly MessageType Type;

    public CryomachineSimpleUiMessage(MessageType type)
    {
        Type = type;
    }
}

/* Currently working on this (UI state)
[Serializable, NetSerializable]
public sealed class Cryomachine
*/
