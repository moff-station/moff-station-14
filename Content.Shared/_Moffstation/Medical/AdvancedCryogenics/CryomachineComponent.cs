using Content.Shared.Body;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CryomachineComponent : Component
{
    #region slots

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
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [DataField]
    public string PortName = "port";

    /// <summary>
    /// The sound emitted when we shock the brain.
    /// </summary>
    [DataField]
    public SoundSpecifier ShockSound = new SoundPathSpecifier("/Audio/Effects/tesla_consume.ogg");

    /// <summary>
    /// The sound emitted when we detach the capsule.
    /// </summary>
    [DataField]
    public SoundSpecifier DetachSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>
    /// The organ categories that will be monitored in the UI
    /// </summary>
    [DataField]
    public List<ProtoId<OrganCategoryPrototype>> MonitoredOrgans = new();


    /// <summary>
    /// Time interval between two UI updates
    /// </summary>
    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUiUpdate = TimeSpan.FromSeconds(0);


}

/// <summary>
/// This is used to handle entities that can be fit inside another entity with <see cref="CryomachineComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class CryocapsuleComponent : Component { }


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
