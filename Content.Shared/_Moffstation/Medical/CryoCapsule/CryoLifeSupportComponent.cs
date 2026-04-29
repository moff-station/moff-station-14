using System.Linq;
using Content.Shared._Moffstation.Body.Components;
using Content.Shared.Body;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This is used for machine that can keep organs inside a <see cref="CryoCapsuleComponent"/> alive.
/// </summary>
[RegisterComponent, NetworkedComponent]
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
    /// The name of each organs when displayed in the UI.
    /// </summary>
    [DataField]
    public List<string> MonitoredOrganNames;

    /// <summary>
    /// Sound emitted when the capsule is detached
    /// </summary>
    [DataField]
    public SoundSpecifier? DetachCapsuleSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>
    /// Sound emitted when the brain is revived
    /// </summary>
    [DataField]
    public SoundSpecifier? ReviveBrainSound = new SoundPathSpecifier("/Audio/Effects/tesla_consume.ogg");


    /// <summary>
    /// Time between two subsequent UI updates
    /// </summary>
    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan UiNextUpdateTime = TimeSpan.Zero;

    /// <summary>
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [DataField]
    public string PortName = "port";
}


/// <summary>
/// This is used for entities that can be inserted inside a Cryogenic Life Support machine.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FitInCryoLifeSupportComponent : Component;


/// <summary>
/// Raised on an entity to obtain the status of their organs (assumed absent by default).
/// </summary>
[ByRefEvent]
public sealed class OrganStatusQueryEvent : EntityEventArgs
{
    public List<OrganEntry> OrganEntries;

    public OrganStatusQueryEvent(List<ProtoId<OrganCategoryPrototype>> organs)
    {
        OrganEntries = organs.Select(category => new OrganEntry("unknown", category, OrganEntry.OrganStatus.Absent)).ToList();
    }
}



/// <summary>
/// Raised on an entity when the brain is being reactivated.
/// </summary>
public sealed class ReviveBrainEvent : EntityEventArgs;
