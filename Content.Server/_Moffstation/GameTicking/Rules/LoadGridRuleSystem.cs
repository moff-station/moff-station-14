using System.Numerics;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class LoadGridRuleSystem : GameRuleSystem<LoadGridRuleComponent>
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;


    protected override void Started(EntityUid uid, LoadGridRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        //Get the station
        if (!TryGetRandomStation(out var station) ||
            !TryComp<StationDataComponent>(station, out var data))
        {
            Log.Warning($"Unable to find a valid station for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the map that the main station exists on
        if (_stationSystem.GetLargestGrid(station.Value) is not { } largestGrid)
        {
            Log.Warning($"Unable to find map for GameRule {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }
        var map = Transform(largestGrid).MapID;
        if (map == MapId.Nullspace)
        {
            Log.Warning($"Attempted to load into nullspace for GameRule {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the next offset of the grid, make sure there are no collisions
        var stationLocation = _transform.GetWorldPosition(largestGrid);
        var offset = Vector2.Zero;

        var attempts = 0;
        while (offset == Vector2.Zero)
        {
            var currentOffset = stationLocation + RobustRandom.NextVector2(component.MinimumDistance, component.MaximumDistance);
            var safetyBounds = Box2.UnitCentered.Enlarged(component.SafetyZoneRadius);
            if (!HasCollisions(map, safetyBounds.Translated(currentOffset)))
                offset = currentOffset;
            else if (attempts > component.MaxAttempts)
            {
                Log.Warning($"Unable to find unobstructed location for GameRule {args.RuleId}!");
                ForceEndSelf(uid, gameRule);
                return;
            }
            attempts++;
        }

        // Load the grid
        if (!_mapLoader.TryLoadGrid(map, component.GridPath, out var spawnedGrid, null, offset))
        {
            Log.Warning($"Unable to load grid for GameRule {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        var ev = new RuleLoadedGridsEvent(map, [spawnedGrid.Value.Owner]);
        RaiseLocalEvent(uid, ref ev);
    }

    private List<Entity<MapGridComponent>> _mapGrids = new();
    private bool HasCollisions(MapId mapId, Box2 point)
    {
        _mapGrids.Clear();
        _mapManager.FindGridsIntersecting(mapId, point, ref _mapGrids);
        return _mapGrids.Count > 0;
    }
}
