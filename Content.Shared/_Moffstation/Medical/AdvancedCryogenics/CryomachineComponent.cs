using Content.Shared.Containers.ItemSlots;

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
