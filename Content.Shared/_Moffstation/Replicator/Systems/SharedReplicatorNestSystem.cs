using System.Linq;
using Content.Shared._Impstation.SpawnedFromTracker;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared.Audio;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chasm;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Station;
using Content.Shared.Storage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Replicator.Systems;

/// This system handles the myriad behaviors of <see cref="ReplicatorNestComponent"/>, including point calculation,
/// level tracking, and managing member replicators.
public abstract partial class SharedReplicatorNestSystem : EntitySystem
{
    [Dependency] protected ReplicatorHiveLeaderSystem HiveLeader = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] protected SharedContainerSystem ContainerSystem = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] protected MobStateSystem MobState = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private InventorySystem _inventory = default!;

    [Dependency] private EntityQuery<BuckleComponent> _buckleQuery;
    [Dependency] private EntityQuery<ReplicatorComponent> _replicatorQuery;
    [Dependency] private EntityQuery<StrapComponent> _strapQuery;
    [Dependency] private EntityQuery<EntityStorageComponent> _storageQuery;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery;

    private static readonly EntProtoId<ReplicatorNestComponent> NestProtoId = "ReplicatorNest";

    /// Creates and returns a new replicator nest at the given <paramref name="location"/>, assigning the given
    /// <paramref name="replicatorsToAssignToNest"/> to it.
    [PublicAPI]
    public Entity<ReplicatorNestComponent> SpawnNest(
        EntityCoordinates location,
        IEnumerable<Entity<ReplicatorComponent>> replicatorsToAssignToNest
    )
    {
        // Make the new nest, assigning the given replicators to it.
        var nest = EntityManager.PredictedSpawnAtPosition<ReplicatorNestComponent>(NestProtoId, location);
        HiveLeader.AssignReplicators(nest.Owner, replicatorsToAssignToNest);

        var createdEv = new ReplicatorNestCreatedEvent();
        RaiseLocalEvent(nest, ref createdEv);

        return nest;
    }


    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ReplicatorNestComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<ChasmComponent>(ent);
        EnsureComp<ReplicatorHiveLeaderComponent>(ent);

        ent.Comp.Hole = ContainerSystem.EnsureContainer<Container>(ent, ent.Comp.HoleContainerId);

        ent.Comp.SizePoints = ent.Comp.UpgradeCostPerLevel * ent.Comp.CurrentLevel;
        ent.Comp.SpawnPoints = ent.Comp.SpawnCostPerExistingSpawn;
        ent.Comp.TileConversionPoints = ent.Comp.TileConversionCost;
        Dirty(ent);
    }

    /// Prevents living things from falling into the nest.
    [SubscribeLocalEvent]
    private void OnTryStartFalling(Entity<ReplicatorNestComponent> ent, ref EntityStartFallingAttemptEvent args)
    {
        if (MobState.IsAlive(args.Faller))
        {
            args.Cancelled = true;
        }
    }

    /// Anything that tries to fall into the hole but doesn't "fit" gets thrown away from the hole.
    [SubscribeLocalEvent]
    private void OnChasmRejects(Entity<ReplicatorNestComponent> ent, ref FallerRejectedByChasmEvent args)
    {
        if (TryComp<PullableComponent>(args.Entity, out var pullable) && pullable.BeingPulled)
            _pulling.TryStopPull(args.Entity, pullable);

        var nestPosition = _xform.GetWorldPosition(Transform(ent), _transformQuery);
        var fallerPosition = _xform.GetWorldPosition(args.Entity, _transformQuery);
        _throwing.TryThrow(args.Entity, (fallerPosition - nestPosition) * 10, 7, ent, 0);
    }

    [SubscribeLocalEvent]
    private void OnEntityStartedFallingIntoReplicatorNest(
        Entity<ReplicatorNestComponent> entity,
        ref EntityStartedFallingIntoChasmEvent args
    )
    {
        if (TryComp<PullableComponent>(args.Faller, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(args.Faller, pullable);
        }

        // Unbuckle anything attached.
        foreach (var buckledEnt in _buckleQuery.ResolveAll(
                     _strapQuery.ResolveOrNull(args.Faller, false)?.Comp.BuckledEntities ?? []
                 ))
        {
            _buckle.Unbuckle(buckledEnt.AsNullable(), user: null);
        }

        // no funny business
        _stun.TryKnockdown(args.Faller.Owner, args.Faller.Comp.DeletionTime, false);
    }

    /// When something falls into the hole, calculate points, try spawning, etc.
    [SubscribeLocalEvent]
    private void OnEntityCompletedFallingIntoReplicatorNest(
        Entity<ReplicatorNestComponent> entity,
        ref EntityCompletedFallingIntoChasmEvent args
    ) => Consume(entity, args.Faller);

    private void Consume(Entity<ReplicatorNestComponent> entity, EntityUid rootConsumed)
    {
        foreach (var consumed in AllConsumedEntitiesRecursive(rootConsumed))
        {
            Log.Debug($"Replicator nest {ToPrettyString(entity)} consumed entity {ToPrettyString(consumed)}");

            if (_mind.TryGetMind(consumed, out _, out _) &&
                _whitelist.CheckBoth(consumed, entity.Comp.PreservationBlacklist, entity.Comp.PreservationWhitelist))
            {
                // Preserve entities with a mind or which pass the lists.
                ContainerSystem.Insert(consumed, entity.Comp.Hole);
                // TODO Force-stunning things in the hole is pretty hacky
                // used stunned to prevent any funny being done inside the pit
                EnsureComp<StunnedComponent>(consumed);
            }
            else
            {
                // ... otherwise just delete them.
                PredictedQueueDel(consumed);
            }

            AddPoints(entity, consumed);
        }

        CheckSizePoints(entity);
        CheckSpawnPoints(entity);
        CheckTileConversionPoints(entity);
    }

    [SubscribeLocalEvent]
    private void OnEntRemoved(Entity<ReplicatorNestComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Undo the jank "lmao you can't act in the hole" stun.
        RemCompDeferred<StunnedComponent>(args.Entity);
        _stun.TryKnockdown(args.Entity, TimeSpan.FromSeconds(2), false);
    }

    [SubscribeLocalEvent]
    private void OnTrackedSpawnerUsed(Entity<ReplicatorNestComponent> ent, ref TrackedSpawnerUsed args)
    {
        ent.Comp.UnclaimedSpawners.Remove(args.Spawner);
        Dirty(ent);

        if (_replicatorQuery.ResolveOrNull(args.Spawned) is { } replicator)
        {
            HiveLeader.AssignReplicators(ent.Owner, [replicator]);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<ReplicatorNestComponent> ent, ref ComponentRemove args)
    {
        // Delete unclaimed spawners.
        foreach (var spawner in ent.Comp.UnclaimedSpawners)
        {
            PredictedQueueDel(spawner);
        }
    }

    /// Implements special point calculation for stacks, by count.
    [SubscribeLocalEvent]
    private static void StackComponentSpecialPoints(
        Entity<StackComponent> entity,
        ref ReplicatorNestSpecialPointCalculationEvent args
    )
    {
        args.SizePoints += entity.Comp.Count;
        args.SpawningPoints += entity.Comp.Count;
    }

    /// Spawns a ghostrole spawner and tracks it on the nest.
    private void SpawnNew(Entity<ReplicatorNestComponent> ent)
    {
        var spawned = EntityManager.PredictedSpawnAtPosition<SpawnedFromTrackerComponent>(
            ent.Comp.ToSpawn,
            Transform(ent).Coordinates
        );
        spawned.Comp.SpawnedFrom = ent;
        ent.Comp.UnclaimedSpawners.Add(spawned);
        Dirty(ent);

        var spawnerEv = new ReplicatorSpawnerCreatedEvent();
        RaiseLocalEvent(ent, ref spawnerEv);
    }

    protected abstract void ConvertTiles(Entity<ReplicatorNestComponent> ent, float radius);


    private IEnumerable<EntityUid> AllConsumedEntitiesRecursive(EntityUid rootConsumed)
    {
        // I sure hope we don't somehow have a cycle :^)
        return GetInventory().Concat(GetStorage()).Append(rootConsumed).Distinct();

        IEnumerable<EntityUid> GetInventory()
        {
            var inventory = _inventory.GetSlotEnumerator(rootConsumed);
            while (inventory.NextItem(out var inventoryItem))
            {
                yield return inventoryItem;
            }
        }

        IEnumerable<EntityUid> GetStorage()
        {
            if (_storageQuery.TryComp(rootConsumed, out var storage))
            {
                foreach (var contentsContainedEntity in storage.Contents.ContainedEntities)
                {
                    yield return contentsContainedEntity;
                }
            }
        }
    }
}
