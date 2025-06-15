using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Vampire.EntitySystems;

public sealed class BurnedBySunSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var enumerator = EntityQueryEnumerator<BurnedBySunComponent>();
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            Update(uid, comp, time);
        }
    }

    protected void Update(EntityUid uid, BurnedBySunComponent comp, TimeSpan time)
    {
        if (time < comp.NextUpdate)
            return;

        comp.NextUpdate += comp.UpdateInterval;

        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            Damage(uid, comp); // we aren't on a grid, time to burn
            return;
        }

        if (comp.TileBlacklist.Count == 0)
            return;

        var tileBlacklist = new List<ITileDefinition>();
        foreach (var tileProto in comp.TileBlacklist)
        {
            if (_tileDefs.TryGetDefinition(tileProto, out var tileDef))
                tileBlacklist.Add(tileDef);
        }

        var tileRef = _map.GetTileRef(xform.GridUid.Value,
            grid,
            new EntityCoordinates(uid, xform.Coordinates.Position));

        _tileDefs.TryGetDefinition(tileRef.Tile.TypeId, out var currTileDef);
        if (currTileDef is null || tileBlacklist.Contains(currTileDef))
            Damage(uid, comp);
    }

    private void Damage(EntityUid uid, BurnedBySunComponent comp)
    {
        if (!TryComp<DamageableComponent>(uid, out var dmgComp))
            return;

        // Make it ramp up in severity over time.
        if (comp.LastBurn >= comp.NextUpdate - comp.UpdateInterval)
            comp.Accumulation = Math.Clamp(comp.Accumulation + 0.2f, 0.0f, 1.0f);
        else
            comp.Accumulation = 0.0f;

        _damage.TryChangeDamage(uid, comp.Damage * comp.Accumulation);
        comp.LastBurn = _timing.CurTime;
    }
}
