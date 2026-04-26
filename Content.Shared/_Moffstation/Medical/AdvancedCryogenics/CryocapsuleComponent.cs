using Content.Shared.Body;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used to handle entities who's internal organs are exposed and can be placed / removed.
/// </summary>
[RegisterComponent]
public sealed partial class CryocapsuleComponent : Component
{
    /// <summary>
    /// Organ types that can be inserted inside the capsule.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> OrganWhitelist = new HashSet<ProtoId<OrganCategoryPrototype>>()
    {

    };

    /// <summary>
    /// Organs already present in the body.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> Organs = new();
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
