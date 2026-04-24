using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CryomachineComponent : Component
{
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

}

[Serializable, NetSerializable]
public enum CryomachineUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CryomachineSimpleUiMessage : BoundUserInterfaceMessage
{
    public enum MessageType { JumpstartBrain, EjectBeaker }

    public readonly MessageType Type;

    public CryomachineSimpleUiMessage(MessageType type)
    {
        Type = type;
    }
}
