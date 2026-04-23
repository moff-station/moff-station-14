using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CryoCasketComponent : Component
{
    #region organs slots

    /// <summary>
    /// The ID of the itemslot that holds the brain.
    /// </summary>
    [DataField]
    public string BrainSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> for this capsule. Holds the brain.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot BrainSlot = new();

    #endregion
}
