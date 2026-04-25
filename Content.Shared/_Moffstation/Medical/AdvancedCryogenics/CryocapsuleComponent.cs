using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CryocapsuleComponent : Component
{
    #region organs slots

    /// <summary>
    /// The ID of the <see cref="ItemSlot"/> that holds the brain.
    /// </summary>
    [DataField]
    public string BrainSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that holds the brain.
    /// </summary>
    [DataField(required: true)]
    public ItemSlot BrainSlot = new();

    /// <summary>
    /// The ID of the <see cref="ItemSlot"/> that holds the eyes.
    /// </summary>
    [DataField]
    public string EyesSlotId = "eyes_slot";

    [DataField]
    public ItemSlot EyesSlot = new();

    /// <summary>
    /// The ID of the <see cref="ItemSlot"/> that holds the lungs.
    /// </summary>
    [DataField]
    public string LungSlotId = "lung_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that holds the lungs.
    /// </summary>
    [DataField]
    public ItemSlot LungSlot = new();

    /// <summary>
    /// The ID of the <see cref="ItemSlot"/> that holds the lungs.
    /// </summary>
    [DataField]
    public string HeartSlotId = "heart_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that holds the lungs.
    /// </summary>
    [DataField]
    public ItemSlot HeartSlot = new();


    /// <summary>
    /// The ID of the <see cref="ItemSlot"/> that holds the stomach.
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


/// <summary>
/// Contain information on the capsule
/// </summary>
[Serializable, NetSerializable]
public readonly record struct CryoCapsuleEntry
{
    public readonly bool BrainPresent;
    public readonly bool EyesPresent;
    public readonly bool LungPresent;
    public readonly bool HeartPresent;
    public readonly bool StomachPresent;

    public CryoCapsuleEntry(bool brainPresent, bool eyesPresent, bool lungPresent, bool heartPresent, bool stomachPresent)
    {
        BrainPresent = brainPresent;
        EyesPresent = eyesPresent;
        LungPresent = lungPresent;
        HeartPresent = heartPresent;
        StomachPresent = stomachPresent;
    }
}


[Serializable, NetSerializable]
public enum CryocapsuleVisuals : byte
{
    BrainPresent,
    HasMind
}

[Serializable, NetSerializable]
public enum CryocapsuleVisualLayers : byte
{
    Brain,
    Eyes,
    Lungs,
    Heart,
    Stomach,
    Base
}
