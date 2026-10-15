using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery;

    public bool TryGetTilePuddle(EntityCoordinates coords, [NotNullWhen(true)] out EntityUid? puddle)
    {
        puddle = null;
        if (!_turf.TryGetTileRef(coords, out var tile) || !_mapGridQuery.TryComp(tile.Value.GridUid, out var grid))
            return false;

        var anchored = _map.GetAnchoredEntities((tile.Value.GridUid, grid), tile.Value.GridIndices);
        while (anchored.MoveNext(out var ent))
        {
            if (!_puddleQuery.HasComp(ent))
                continue;

            puddle = ent;
            return true;
        }
        return false;
    }
}
