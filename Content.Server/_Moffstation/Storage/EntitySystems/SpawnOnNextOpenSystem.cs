using Content.Server._Moffstation.Storage.Components;
using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Storage.EntitySystems;

/// <summary>
/// Handles <see cref="SpawnOnNextOpenComponent"/>, a component which causes attached
/// <see cref="EntityStorageComponent"/> to effectively contain additional entities by spawning the entities when the
/// container is first opened. After the entities are spawned, the component is removed. This is specifically useful for
/// cases where contents need to be "added" to a container, but the container may be full, or the added contents are
/// mobs we don't want to suffocate in an air-tight crate.
/// </summary>
public sealed class SpawnOnNextOpenSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnNextOpenComponent, StorageAfterOpenEvent>(OnStorageAfterOpenEvent);
    }

    private void OnStorageAfterOpenEvent(Entity<SpawnOnNextOpenComponent> entity, ref StorageAfterOpenEvent args)
    {
        var coords = Transform(entity).Coordinates;
        foreach (var protoId in entity.Comp.Entities)
        {
            Spawn(protoId, coords);
        }

        RemComp<SpawnOnNextOpenComponent>(entity);
    }

    public void AddEntitiesToSpawnOnFirstOpen(Entity<EntityStorageComponent> entity, IEnumerable<EntProtoId> entities)
    {
        var entProtoIds = EnsureComp<SpawnOnNextOpenComponent>(entity).Entities;
        entProtoIds.AddRange(entities);
    }
}
