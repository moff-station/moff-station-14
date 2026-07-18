using Content.Server.Ghost.Roles.Events;
using Content.Shared._Impstation.SpawnedFromTracker;

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
