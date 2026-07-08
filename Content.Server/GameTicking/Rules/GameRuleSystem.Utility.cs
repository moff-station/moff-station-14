using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared._Moffstation.Pirate.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T: IComponent
{
    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent> QueryDelayedRules()
    {
        return EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent>();
    }

    /// <summary>
    /// Queries all gamerules, regardless of if they're active or not.
    /// </summary>
    protected EntityQueryEnumerator<T, GameRuleComponent> QueryAllRules()
    {
        return EntityQueryEnumerator<T, GameRuleComponent>();
    }

    /// <summary>
    ///     Utility function for finding a random event-eligible station entity
    /// </summary>
    protected bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            // Moffstation - Start - Pirate "stations" are not real stations
            // TODO: Fix pirates so we don't have to use this hack
            if (HasComp<PirateStationComponent>(uid))
                continue;
            // Moffstation - End

            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[RobustRandom.Next(stations.Count)];
        return true;
    }

    protected bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out EntityUid? targetStation,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords,
        // Moffstation - Add Largestgrid and safeatmos options
        bool largestGrid = false,
        bool safeAtmos = false)
        // Moffstation - End
    {
        tile = default;
        targetStation = EntityUid.Invalid;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation((targetStation.Value, Comp<StationDataComponent>(targetStation.Value)),
                out tile,
                out targetGrid,
                out targetCoords,
                // Moffstation - Added largestGrid and safeatmos options
                    largestGrid,
                    safeAtmos);
                // Moffstation - End
        }

        return false;
    }

    protected bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords,
        // Moffstation - Add Largestgrid and safeatmos options
        bool largestGrid = false,
        bool safeAtmos = false)
        // Moffstation - End
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = EntityUid.Invalid;

        // Weight grid choice by tilecount
        var weights = new Dictionary<Entity<MapGridComponent>, float>();
        foreach (var possibleTarget in station.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(possibleTarget, out var comp))
                continue;

            weights.Add((possibleTarget, comp), _map.GetAllTiles(possibleTarget, comp).Count());
        }

        if (weights.Count == 0)
        {
            targetGrid = EntityUid.Invalid;
            return false;
        }

        (targetGrid, var gridComp) = RobustRandom.Pick(weights);

        var found = false;
        var aabb = gridComp.LocalAABB;

        for (var i = 0; i < 100; i++) //Moffstation paradox clone integration test fix
        {
            var randomX = RobustRandom.Next((int) aabb.Left, (int) aabb.Right);
            var randomY = RobustRandom.Next((int) aabb.Bottom, (int) aabb.Top);

            tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile)
                || _atmosphere.IsTileAirBlockedCached(targetGrid, tile)
                // Moffstation - Start - Add Largestgrid and safeatmos options
                || _station.GetLargestGrid(station.Owner) != targetGrid && largestGrid
                || Transform(targetGrid).MapUid is not { } map
                || !_atmosphere.IsTileMixtureProbablySafe(targetGrid, map, tile) && safeAtmos)
                // Moffstation - End
            {
                continue;
            }

            found = true;
            targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tile);
            break;
        }

        // Moffstation - Start - Paradox Clone integration test fix
        // Fallback: iterate all tiles on the grid systematically, picking a random one as the target coords.
        if (!found)
        {
            Log.Warning(
                "Failed to pick suitable random location for paradox clone spawn. Resorting to brute enumeration of tiles");
            var tGrid = targetGrid; // Rebind to local to use in lambdas.
            var map = Transform(tGrid).MapUid;
            var allValidCoords = _map.GetAllTiles(targetGrid, gridComp)
                .Select(t => t.GridIndices)
                .Where(index => !(_atmosphere.IsTileSpace(tGrid, map, index)
                                  || _atmosphere.IsTileAirBlockedCached(tGrid, index))
                                // Moffstation - Start - Add Largestgrid and safeatmos options
                                || _station.GetLargestGrid(station.Owner) == tGrid && largestGrid
                                || map != null
                                || _atmosphere.IsTileMixtureProbablySafe(tGrid, map!.Value, index) && safeAtmos)
                                // Moffstation - End
                .ToList();

            RobustRandom.Shuffle(allValidCoords);
            if (allValidCoords.TryFirstOrNull(out var first))
            {
                found = true;
                targetCoords = _map.GridTileToLocal(tGrid, gridComp, first.Value);
            }
        }
        // Moffstation - End

        return found;
    }

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }
}
