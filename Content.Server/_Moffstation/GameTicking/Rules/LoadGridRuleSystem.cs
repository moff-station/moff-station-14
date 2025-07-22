using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class LoadGridRuleSystem : GameRuleSystem<LoadGridRuleComponent>
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Added(EntityUid uid, LoadGridRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        //Get the station
        if (!TryGetRandomStation(out var station) ||
            !TryComp<StationDataComponent>(station, out var data))
        {
            Log.Warning($"Unable to find a valid station for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the map that the main station exists on
        if (_stationSystem.GetLargestGrid(data) is not { } largestGrid)
        {
            Log.Warning($"Unable to find map for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }
        var map = Transform(largestGrid).MapID;
        if (map == MapId.Nullspace)
        {
            Log.Warning($"Attempted to load into nullspace for rule {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the next offset of the grid
        var stationLocation = _transform.GetWorldPosition(largestGrid);
        var offset = stationLocation + RobustRandom.NextVector2(component.MinimumDistance, component.MaximumDistance);

        // Load the grid
        if (!_mapLoader.TryLoadGrid(map, component.GridPath, out _, null, offset))
        {
            Log.Warning($"Unable to load grid for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
        }
    }
}
