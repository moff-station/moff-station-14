using Content.Shared.Damage;
using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Components;

/// <summary>
/// Entities with this component are damaged periodically if they are either off grid, or over particular tiles.
/// The intended use for this is for vampires who should remain out of the sunlight.
/// </summary>
/// <remarks>
/// It's important to note that this does not allow for protective outerwear like hardsuits to prevent an
/// entity from burning in the sun. This is intentional.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class BurnedBySunComponent : Component
{
    /// <summary>
    /// A list of tiles that when stood on, cause the entity to start ticking up <see cref="Accumulation"/>
    /// </summary>
    [DataField]
    public List<ProtoId<ContentTileDefinition>> TileBlacklist = new();

    /// <summary>
    /// The current cache of the tile blacklist so we're not updating it every update cycle.
    /// </summary>
    [DataField]
    public List<ITileDefinition> TileBlacklistCache = new();

    /// <summary>
    /// The field which keeps track of when the next tick of damage will occur.
    /// </summary>
    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Time between updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Previous time when the entity was lasted burned, this is used to reset <see cref="Accumulation"/> and generally
    /// shouldn't be modified externally.
    /// </summary>
    [DataField]
    public TimeSpan LastBurn = TimeSpan.Zero;

    /// <summary>
    /// The percentage of <see cref="Damage"/> to perform per update.
    /// This value is always clamped between 0.0 and 1.0, and increases by <see cref="AccumulationPerUpdate"/> per update until it hits 1.0 (100%).
    /// And resets to 0.0 if the time since <see cref="LastBurn"/> is greater than <see cref="UpdateInterval"/>
    /// </summary>
    /// <remarks>
    /// This is largely to give the entity some time to return to safety before they start taking full damage from the sun.
    /// </remarks>
    [DataField]
    public float Accumulation;

    /// <summary>
    /// The amount to increase <see cref="Accumulation"/> per update.
    /// </summary>
    [DataField]
    public float AccumulationPerUpdate = 0.05f;

    /// <summary>
    /// The damage to take per update. Note that this value is multiplied with the current <see cref="Accumulation"/>.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Amount of firestacks to give the entity while they're in sunlight.
    /// This value is multiplied by <see cref="Accumulation"/>.
    /// </summary>
    [DataField]
    public float FireStacksPerUpdate = 1.0f;

    /// <summary>
    /// The sound that plays when the vampire is getting burned in the sun
    /// </summary>
    [DataField]
    public SoundSpecifier BurnSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    public override bool SendOnlyToOwner => true;
}
