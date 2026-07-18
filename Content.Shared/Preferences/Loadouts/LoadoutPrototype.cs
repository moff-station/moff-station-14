using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Individual loadout item to be applied.
/// </summary>
[Prototype]
public sealed partial class LoadoutPrototype : IPrototype, IEquipmentLoadout
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// A text identifier used to group loadouts.
    /// </summary>
    [DataField]
    public string? GroupBy;
    /*
     * You can either use an existing StartingGearPrototype or specify it inline to avoid bloating yaml.
     */

    /// <summary>
    /// An entity whose sprite, name and description is used for display in the interface. If null, tries to get the proto of the item from gear (if it is a single item).
    /// </summary>
    [DataField]
    public EntProtoId? DummyEntity;

    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    // Moffstation - Begin
    /// If not null, the loadout option's name will be this as opposed to trying to get it from any other source.
    [DataField]
    public LocId? NameOverride;
    // Moffstation - End

    /// <summary>
    /// Effects to be applied when the loadout is applied.
    /// These can also return true or false for validation purposes.
    /// </summary>
    [DataField]
    public List<LoadoutEffect> Effects = new();

    /// <inheritdoc />
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    public List<EntProtoId> Inhand { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();

    // Moffstation - Begin - Special loadouts; enables eg. personal items on cyborgs
    /// A dictfrom <see cref="JobPrototype.JobEntity"/> to special loadouts to apply to them.
    [DataField]
    public Dictionary<ProtoId<RoleLoadoutPrototype>, SpecialLoadout> SpecialJobLoadouts = new();
    // Moffstation - End
}

// Moffstation - Begin - Special loadouts enable loadouts for non-humanoid jobs. I hope that we get to delete this someday thanks to comprehensive loadout improvements!
[DataDefinition]
public sealed partial class SpecialLoadout : IEquipmentLoadout
{
    [DataField]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    [DataField]
    public List<EntProtoId> Inhand { get; set; } = new();

    [DataField]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();

    /// <summary>
    /// Some entities (ex: Cyborgs) may have loadout options that are not stored in inventory or storage but in a container
    /// inside the entity (ex: MMI and Positronic Brain)
    /// </summary>
    [DataField]
    public Dictionary<string, List<EntProtoId>> Containers = new();
}
// Moffstation - End
