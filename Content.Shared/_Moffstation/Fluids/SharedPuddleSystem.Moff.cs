using System.Diagnostics.CodeAnalysis;
using Content.Shared._Funkystation.Footprints;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [Dependency] private EntityQuery<FootprintComponent> _footprintQuery;

    /// <summary>
    /// Finds the puddle anchored on the tile at the coordinates, falling back to its footprints if there's no puddle.
    /// </summary>
    public bool TryGetTilePuddle(EntityCoordinates coords, [NotNullWhen(true)] out EntityUid? puddle)
    {
        puddle = null;
        if (!_turf.TryGetTileRef(coords, out var tile)
            || !TryComp<MapGridComponent>(tile.Value.GridUid, out var grid))
            return false;

        var anchored = _map.GetAnchoredEntitiesEnumerator(tile.Value.GridUid, grid, tile.Value.GridIndices);
        while (anchored.MoveNext(out var ent))
        {
            if (!_puddleQuery.HasComp(ent))
                continue;

            puddle = ent;
            if (!_footprintQuery.HasComp(ent))
                return true;
        }

        return puddle != null;
    }
}
