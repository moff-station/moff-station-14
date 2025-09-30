using System.Linq;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GridPreloader;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
//Admin Logging stuff
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class LoadAbsentMapRuleSystem : GameRuleSystem<LoadAbsentMapRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    protected override void Added(EntityUid uid, LoadAbsentMapRuleComponent comp, GameRuleComponent rule, GameRuleAddedEvent args) // I DON'T KNOW WHAT THIS DOES I JUST STOLE IT FROM LOADMAPRULE
    {

        //If the map already Exists, Don't load a duplicate!
        var mapExist = false;
        foreach (var checkMapId in _mapSystem.GetAllMapIds())
        {
            if (!_mapSystem.TryGetMap(checkMapId, out var mapUid))
                continue;
            if (_entManager.GetComponent<MetaDataComponent>(mapUid.Value).EntityName == comp.MapName && mapUid != null)
            {
                mapExist = true;
            }

        }
        if (mapExist)
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Low, $"The map {comp.MapName} already exists!");
            ForceEndSelf(uid, rule);
            return;
        }

        MapId mapId;
        IReadOnlyList<EntityUid> grids;
        if (comp.GameMap != null)
        {
            // Component has one of three modes, only one of the three fields should ever be populated.

            var gameMap = _prototypeManager.Index(comp.GameMap.Value);
            grids = GameTicker.LoadGameMap(gameMap, out mapId, null);
            Log.Info($"Created map {mapId} for {ToPrettyString(uid):rule}");
        }
        else if (comp.MapPath is { } path)
        {

            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            if (!_mapLoader.TryLoadMap(path, out var map, out var gridSet, opts))
            {
                Log.Error($"Failed to load map from {path}!");
                ForceEndSelf(uid, rule);
                return;
            }

            grids = gridSet.Select(x => x.Owner).ToList();
            mapId = map.Value.Comp.MapId;
        }
        else
        {
            Log.Error($"No valid map prototype or map path associated with the rule {ToPrettyString(uid)}");
            ForceEndSelf(uid, rule);
            return;
        }

        var ev = new RuleLoadedGridsEvent(mapId, grids);
        RaiseLocalEvent(uid, ref ev);

        base.Added(uid, comp, rule, args);
    }
}