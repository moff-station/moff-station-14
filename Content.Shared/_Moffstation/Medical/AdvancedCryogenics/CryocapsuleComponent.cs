using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CryocapsuleComponent : Component
{
    /// <summary>
    /// The ID of the itemslot that holds the cryo unit
    /// </summary>
    [DataField]
    public string CasketSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> for this capsule. Holds the brain.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot CasketSlot = new();
}
