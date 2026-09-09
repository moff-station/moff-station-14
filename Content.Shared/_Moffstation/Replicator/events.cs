using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Replicator;

/// This event is raised on an entity with <see cref="ReplicatorComponent"/> to enable its ability to upgrade.
[ByRefEvent]
public readonly record struct EnableReplicatorUpgradesEvent;

/// This event is raised on <see cref="ReplicatorComponent.HiveLeader"/> when a replicator is upgraded.
[ByRefEvent]
public readonly record struct MemberReplicatorUpgradedEvent(
    Entity<ReplicatorComponent> UpgradedFrom,
    Entity<ReplicatorComponent> UpgradedTo
);

/// This event is raised on entities with <see cref="ReplicatorComponent"/> when its leader changes, for example when
/// a queen creates a nest.
[ByRefEvent]
public readonly record struct HiveLeaderAssignedEvent(Entity<ReplicatorHiveLeaderComponent> Leader);

/// This event is raised on entities when they fall into an entity with <see cref="ReplicatorNestComponent"/> and when
/// the nest's <see cref="ReplicatorNestComponent.PointEntries"/> for it indicate that it requires special calculation.
/// Basically, this just is a hook for C# calculations of points.
[ByRefEvent]
public record struct ReplicatorNestSpecialPointCalculationEvent(int SizePoints, int SpawningPoints);

/// This event is raised on <see cref="ReplicatorHiveLeaderComponent.Members"/> when the hive leader nest is destroyed.
[ByRefEvent]
public readonly record struct NestDestroyedEvent;

/// This event is raised on <see cref="ReplicatorHiveLeaderComponent.Members"/> when the hive leader queen is destroyed.
[ByRefEvent]
public readonly record struct QueenKilledEvent;

/// This event is raised to create a nest at the invoker's location.
public sealed partial class ReplicatorSpawnNestActionEvent : InstantActionEvent;

/// This event is raised to cause a replicator to upgrade to <see cref="NextStage"/>.
public sealed partial class ReplicatorUpgradeActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId<ReplicatorComponent> NextStage;
}

/// Raised on a <see cref="ReplicatorNestComponent"/> entity immediately after it is created.
[ByRefEvent]
public readonly record struct ReplicatorNestCreatedEvent;

/// Raised on a <see cref="ReplicatorNestComponent"/> entity when size points are gained.
[ByRefEvent]
public readonly record struct ReplicatorNestSizePointsGainedEvent(int Points);

/// Raised on a <see cref="ReplicatorNestComponent"/> entity when a new replicator spawner is created.
[ByRefEvent]
public readonly record struct ReplicatorSpawnerCreatedEvent;

