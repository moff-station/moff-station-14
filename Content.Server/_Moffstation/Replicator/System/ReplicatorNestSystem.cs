using System.Linq;
using System.Numerics;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Replicator;
using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared._Moffstation.Replicator.Systems;
using Content.Shared.Destructible;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Replicator.System;

/// <inheritdoc/>
public sealed partial class ReplicatorNestSystem : SharedReplicatorNestSystem
{
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery;
    [Dependency] private ReplicatorSystem _replicator = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplicatorNestComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnDestroyed(Entity<ReplicatorNestComponent> ent, ref DestructionEventArgs args)
    {
        // Empty the hole.
        ContainerSystem.EmptyContainer(ent.Comp.Hole);

        var livingReplicators = HiveLeader.GetMembers(ent.Owner)
            .Where(it => MobState.IsAlive(it))
            .ToList();

        // Bail out early if there're no more replicators.
        if (livingReplicators.Count == 0)
            return;

        // Assign a surviving replicator to be the "lifeboat" queen..
        var lifeboatUnupgraded = _random.Pick(livingReplicators);
        var livingReplicatorsWithoutPick = livingReplicators.Where(it => it.Owner != lifeboatUnupgraded.Owner).ToList();
        var lifeboatUpgraded = _replicator.UpgradeReplicator(lifeboatUnupgraded, ent.Comp.LifeBoatProto.Id);
        HiveLeader.AssignReplicators(lifeboatUpgraded.Owner, livingReplicatorsWithoutPick.Append(lifeboatUpgraded));

        // Inform the surviving replicators of nest destruction.
        var nestDestroyedEvent = new NestDestroyedEvent();
        foreach (var replicator in livingReplicatorsWithoutPick)
        {
            RaiseLocalEvent(replicator, ref nestDestroyedEvent);
        }
    }

    protected override void ConvertTiles(Entity<ReplicatorNestComponent> ent, float radius)
    {
        var xform = Transform(ent);
        if (_mapGridQuery.ResolveOrNull(xform.GridUid, logMissing: false) is not { } grid)
            return;

        if (!_tileDef.TryGetDefinition(ent.Comp.ConversionTile, out var ct) ||
            ct is not ContentTileDefinition convertTile)
        {
            this.AssertOrLogError($"{nameof(ContentTileDefinition)} {ent.Comp.ConversionTile} not found.");
            return;
        }

        var tileEnumerator = _map.GetLocalTilesEnumerator(
            grid,
            grid,
            new Box2(
                xform.Coordinates.Position + new Vector2(-radius, -radius),
                xform.Coordinates.Position + new Vector2(radius, radius)
            )
        );
        var radiusSquared = Math.Pow(radius, 2);
        var nestCenter = xform.Coordinates.Position - new Vector2(0.5f);
        while (tileEnumerator.MoveNext(out var tile))
        {
            if (tile.Tile.TypeId == convertTile.TileId ||
                (tile.GridIndices - nestCenter).LengthSquared() >= radiusSquared ||
                !_random.Prob(ent.Comp.TileConversionChance))
                continue;

            PredictedSpawnAtPosition(ent.Comp.TileConversionVfx, _turf.GetTileCenter(tile));
            _tile.ReplaceTile(tile, convertTile);
            _tile.PickVariant(convertTile);
        }
    }
}
