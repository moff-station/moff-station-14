using Content.Shared.Body;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This is used for machine that can keep organs inside a <see cref="CryoCapsuleComponent"/> alive.
/// </summary>
[RegisterComponent]
public sealed partial class CryoLifeSupportComponent : Component
{
    #region slots
    /// <summary>
    /// The ID of the itemslot that holds the cryocapsule
    /// </summary>
    [DataField]
    public string CapsuleSlotId = "capsule_slot";

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
    /// The organs category that will be displayed inside the UI.
    /// </summary>
    [DataField]
    public List<ProtoId<OrganCategoryPrototype>> MonitoredOrgans;

    /// <summary>
    /// Sound emitted when the capsule is detached
    /// </summary>
    [DataField]
    public SoundSpecifier? DetachCapsuleSound;

    /// <summary>
    /// Sound emitted when the brain is revived
    /// </summary>
    [DataField]
    public SoundSpecifier? ReviveBrainSound;
}


/// <summary>
/// Raised on an entity to obtain the status of their organs.
/// </summary>
public sealed class OrganStatusQueryEvent : EntityEventArgs
{
    public enum OrganStatus { Absent, Unusable, Damaged, Healthy }

    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganStatus> OrgansStatus = new();
}

/// <summary>
/// Raised on an entity when the brain is being reactivated.
/// </summary>
public sealed class ReviveBrainEvent : EntityEventArgs;
