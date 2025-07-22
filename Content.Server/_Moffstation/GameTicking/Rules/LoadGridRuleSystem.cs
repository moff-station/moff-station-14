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


    protected override void Added(EntityUid uid, LoadGridRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station) ||
            !TryComp<StationDataComponent>(station, out var data) ||
            data.Grids.Count < 1)
        {
            Log.Warning($"Unable to find a valid station for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        if (_stationSystem.GetLargestGrid(data) is not { } largestGrid)
            return;
        var map = Transform(largestGrid).MapID;

        if (map == MapId.Nullspace)
        {
            Log.Warning($"Attempted to load grid into nullspace for rule {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        var offset = RobustRandom.NextVector2(component.MinimumDistance, component.MaximumDistance);

        if (!_mapLoader.TryLoadGrid(map, component.GridPath, out _, null, offset))
        {
            Log.Warning($"Unable to load grid for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
        }
    }
}
