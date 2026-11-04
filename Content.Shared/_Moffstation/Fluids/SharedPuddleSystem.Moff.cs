using System.Diagnostics.CodeAnalysis;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery;

    public IEnumerable<Entity<PuddleComponent>> GetTilePuddle(EntityCoordinates coords)
    {
        if (!_turf.TryGetTileRef(coords, out var tile) || !_mapGridQuery.TryComp(tile.Value.GridUid, out var grid))
            yield break;

        var anchored = _map.GetAnchoredEntities((tile.Value.GridUid, grid), tile.Value.GridIndices);
        while (anchored.MoveNext(out var uid))
        {
            if (!_puddleQuery.TryComp(uid, out var comp))
                continue;

            yield return (uid.Value, comp);
        }
    }
}
