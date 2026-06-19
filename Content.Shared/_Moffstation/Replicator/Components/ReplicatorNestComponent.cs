using Content.Shared._Impstation.SpawnedFromTracker;
using Content.Shared._Moffstation.Replicator.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Replicator.Components;

/// This component makes an entity into a replicator nest. Replicator nests spawn replicators (<see cref="ToSpawn"/>),
/// accrue points (<see cref="PointEntries"/>), and grow in level, among other things.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedReplicatorNestSystem))]
public sealed partial class ReplicatorNestComponent : Component
{
    /// If the nest is destroyed, one member will be upgraded to this prototype as a "lifeboat".
    [DataField]
    public EntProtoId<ReplicatorQueenComponent> LifeBoatProto;

    #region Hole and Point Awards

    /// The ID of <see cref="Hole"/>.
    [DataField]
    public string HoleContainerId = "hole";

    /// The container for entities which fall into the nest but aren't destroyed.
    [ViewVariables(VVAccess.ReadOnly)]
    public Container Hole = default!;

    /// Entities which pass this will not be deleted when they fall into the hole.
    [DataField]
    public EntityWhitelist PreservationWhitelist = new();

    /// Entities which fail this will be deleted when falling into the hole.
    [DataField]
    public EntityWhitelist PreservationBlacklist = new();

    /// Descriptions of how many points to award for an entity which falls into the hole.
    /// <seealso cref="ReplicatorNestPointAwardEntry"/>
    [DataField]
    public List<ReplicatorNestPointAwardEntry> PointEntries = [];

    #endregion


    #region NestLevel

    /// How many points are needed per current level to upgrade to the next level.
    [DataField]
    public int UpgradeCostPerLevel = 400;

    /// How many points the nest current has. Resets when levelling up.
    [DataField, AutoNetworkedField]
    public int SizePoints;

    /// Sound to play when the nest levels up.
    [DataField]
    public SoundSpecifier LevelUpSound = new SoundPathSpecifier("/Audio/_Impstation/Ambience/hole_2.ogg");

    /// The nest's current level.
    [DataField(readOnly: true), AutoNetworkedField]
    public int CurrentLevel = 1;

    /// After this level, the nest will not visually change, will instead begin to convert tiles neaby itself, and the
    /// cost to level up becomes flat.
    [DataField]
    public int EndgameLevel = 3;

    /// A mapping of nest levels to announcements to make when the nest reaches that level.
    [DataField, AutoNetworkedField]
    public Dictionary<int, LocId> Announcements = new();

    #endregion


    #region Spawning

    /// How many points are needed to spawn a new <see cref="ToSpawn"/>.
    [DataField]
    public int SpawnCostPerExistingSpawn = 300;

    /// The current number of spawn points.
    [DataField, AutoNetworkedField]
    public int SpawnPoints;

    /// The entity the nest spawns when enough points are accrued.
    [DataField]
    public EntProtoId<SpawnedFromTrackerComponent> ToSpawn = "SpawnPointGhostReplicator";

    /// The spawns currently made by the nest but not taken.
    /// Tracked so that they can be deleted if the nest is destroyed.
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public readonly HashSet<EntityUid> UnclaimedSpawners = [];

    #endregion


    #region TileConversion

    /// How many points are needed to convert a tile near the nest to <see cref="ConversionTile"/>.
    [DataField]
    public int TileConversionCost = 100;

    /// Per individual tile, the chance it will be converted.
    [DataField]
    public float TileConversionChance = 0.05f;

    /// The radius in which tiles are considered for conversion. Scales with nest level.
    [DataField]
    public float TileConversionRadius = 1f;

    /// How much the conversion chance increases per level.
    [DataField]
    public float TileConversionIncrease = 1f;

    /// The tile to convert to.
    [DataField]
    public ProtoId<ContentTileDefinition> ConversionTile = "FloorReplicator";

    /// The effect entity to spawn on a tile when it's converted.
    [DataField]
    public EntProtoId? TileConversionVfx = "ReplicatorFloorSpawnVFX";

    /// The current number of tile conversion points.
    [DataField, AutoNetworkedField]
    public int TileConversionPoints;

    #endregion
}

/// <summary>
/// This record describes the points award to a replicator nest when an entity falls into the nest.
/// Each individual entry is considered whenever any entity falls into the hole.
/// </summary>
/// <seealso cref="ReplicatorNestComponent.PointEntries"/>
/// <param name="Whitelist">Considered with <see cref="Blacklist"/>, entities which pass <see cref="EntityWhitelistSystem.CheckBoth"/> are awared the points from this entry.</param>
/// <param name="Blacklist">Considered with <see cref="Whitelist"/>, entities which pass <see cref="EntityWhitelistSystem.CheckBoth"/> are awared the points from this entry.</param>
/// <param name="SizePoints">The points to add to <see cref="ReplicatorNestComponent.SizePoints"/> and <see cref="ReplicatorNestComponent.TileConversionPoints"/></param>
/// <param name="SpawningPoints">The points to add to add to <see cref="ReplicatorNestComponent.SpawnPoints"/></param>
/// <param name="DebugName">A string identifier for this entry, used to make debugging easier</param>
/// <param name="CalculateSpecial">If true, in addition to the point entries above, <see cref="ReplicatorNestSpecialPointCalculationEvent"/> will be raised on the entity falling into the hold to calculate the points it should provide.</param>
[DataRecord]
public sealed partial record ReplicatorNestPointAwardEntry(
    EntityWhitelist? Whitelist,
    EntityWhitelist? Blacklist,
    ReplicatorNestPointAward? SizePoints,
    ReplicatorNestPointAward? SpawningPoints,
    string DebugName,
    bool CalculateSpecial = false
);

/// A description of how many points to award.
/// <seealso cref="ReplicatorNestPointAwardEntry"/>
[DataRecord]
public sealed partial record ReplicatorNestPointAward(
    int Flat = 0,
    FixedPoint2 ScaledWithLevel = default,
    FixedPoint2 ScaledWithSpawnCost = default
);
