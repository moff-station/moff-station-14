using Content.Shared._Impstation.SpawnedFromTracker;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Roles.Components;

namespace Content.Server._Moffstation.SpawnedFromTracker;

/// This system forwards <see cref="GhostRoleSpawnerUsedEvent"/>s to
/// <see cref="SpawnedFromTrackerComponent.SpawnedFrom"/> when it is used.
public sealed partial class SpawnedFromTrackerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnedFromTrackerComponent, UsedGhostRoleSpawnerEvent>(OnUsedGhostRoleSpawner);
    }

    private void OnUsedGhostRoleSpawner(Entity<SpawnedFromTrackerComponent> entity, ref UsedGhostRoleSpawnerEvent args)
    {
        var ev = new TrackedSpawnerUsed(args.Spawned, entity);
        RaiseLocalEvent(entity.Comp.SpawnedFrom, ref ev);
    }
}

/// This event is raised on the entity with <see cref="GhostRoleMobSpawnerComponent"/> when it is used.
[ByRefEvent]
public readonly record struct UsedGhostRoleSpawnerEvent(EntityUid Spawned);
