using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization;

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
    /// The <see cref="ItemSlot"/> that holds the brain.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot BrainSlot = new();

    /// <summary>
    /// The ID of the itemslot that holds the lungs.
    /// </summary>
    [DataField]
    public string LungSlotId = "lung_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that holds the lungs.
    /// </summary>
    [DataField]
    public ItemSlot LungSlot = new();


    /// <summary>
    /// The ID of the itemslot that holds the stomach.
    /// </summary>
    [DataField]
    public string StomachSlotId = "stomach_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that holds the stomach.
    /// </summary>
    [DataField]
    public ItemSlot StomachSlot = new();

    #endregion
}

[Serializable, NetSerializable]
public enum CryocasketVisuals : byte
{
    BrainPresent,
    HasMind
}

[Serializable, NetSerializable]
public enum CryocasketVisualLayers : byte
{
    Brain,
    Lungs,
    Heart,
    Stomach,
    Base
}
