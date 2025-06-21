using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Vampire.EntitySystems;

/// <summary>
/// This system interfaces with the <see cref="Content.Shared._Moffstation.Vampire.Components.BurnedBySunComponent"/>
/// to deal damage to entities with the component when they are exposed to the sun.
/// </summary>
public sealed class BurnedBySunSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurnedBySunComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BurnedBySunComponent, TileChangedEvent>(OnTileChanged);
    }

    private void OnMapInit(Entity<BurnedBySunComponent> entity, ref MapInitEvent args)
    {
	    UpdateTileset(entity);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var enumerator = EntityQueryEnumerator<BurnedBySunComponent>();
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            if (IsInTheSun((uid, comp), time))
                Damage((uid, comp));
        }
    }

    /// <summary>
    /// Checks if the entity is in the sun.
    /// This is done by first checking if we're even on grid and then checking if any of the tiles we're on
    /// are in the blacklist.
    /// </summary>
    /// <returns>True if we are in the sun, false otherwise.</returns>
    /// <remarks>
    /// This could also include a raycast to the "sun" but that could be annoying and without feedback
    /// it would be hard for the user to know they are in the sun. Especially since we don't have sunlight streaming
    /// through windows or anything (although that would be cool to implement :3)
    /// </remarks>
    private bool IsInTheSun(Entity<BurnedBySunComponent> entity, TimeSpan time)
    {
        if (time < entity.Comp.NextUpdate)
            return false;

        entity.Comp.NextUpdate += entity.Comp.UpdateInterval;

        var xform = Transform(entity);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return true; // we aren't on a grid, time to burn

        if (entity.Comp.TileBlacklistCache.Count == 0)
            return false;

        var tileRef = _map.GetTileRef(xform.GridUid.Value,
            grid,
            new EntityCoordinates(entity, 0.0f, 0.0f));

        _tileDefs.TryGetDefinition(tileRef.Tile.TypeId, out var currTileDef);
        return currTileDef is null || entity.Comp.TileBlacklistCache.Contains(currTileDef);
    }

    /// <summary>
    /// This catches the event for any tile changes in the map.
    /// </summary>
    private void OnTileChanged(Entity<BurnedBySunComponent> entity, ref TileChangedEvent args)
    {
	    UpdateTileset(entity);
    }

    /// <summary>
    /// Updates the tileset cache.
    /// </summary>
    private void UpdateTileset(Entity<BurnedBySunComponent> entity)
    {
        foreach (var tileProto in entity.Comp.TileBlacklist)
        {
            if (_tileDefs.TryGetDefinition(tileProto, out var tileDef)
		        && !entity.Comp.TileBlacklistCache.Contains(tileDef))
		        entity.Comp.TileBlacklistCache.Add(tileDef);
        }
    }

    /// <summary>
    /// Causes damage to the entity according to the component's specified damage.
    /// The damage does ramp up to the full amount based upon the Accumulation and the AccumulationPerUpdate
    /// </summary>
    private void Damage(Entity<BurnedBySunComponent> entity)
    {
        // Make it ramp up in severity over time.
        entity.Comp.Accumulation = entity.Comp.LastBurn >= entity.Comp.NextUpdate - entity.Comp.UpdateInterval * 2.0
            ? Math.Clamp(entity.Comp.Accumulation + entity.Comp.AccumulationPerUpdate, 0.0f, 1.0f)
            : 0.0f;

        // we make the messages and sound play randomly to be more discordant to the user, and to
        // show that it's ramping up in severity.
        if (_random.NextFloat() < entity.Comp.Accumulation * 0.7)
        {
            _popup.PopupEntity(Loc.GetString("vampire-in-sunlight"),
			       entity.Owner,
                   entity.Owner,
			       entity.Comp.Accumulation < 0.5
			       ? PopupType.SmallCaution
			       : PopupType.MediumCaution);
            _audio.PlayPvs(entity.Comp.BurnSound, entity);
        }

        _flammable.AdjustFireStacks(entity.Owner, entity.Comp.FireStacksPerUpdate * entity.Comp.Accumulation);
	    _damage.TryChangeDamage(entity.Owner, entity.Comp.Damage * entity.Comp.Accumulation, true);
        entity.Comp.LastBurn = _timing.CurTime;
    }
}
