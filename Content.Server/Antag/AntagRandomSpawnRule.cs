using Content.Server.Antag.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Robust.Shared.Map;

namespace Content.Server.Antag;

public sealed partial class AntagRandomSpawnSystem : GameRuleSystem<AntagRandomSpawnComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomSpawnComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, AntagRandomSpawnComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        // we have to select this here because AntagSelectLocationEvent is raised twice because MakeAntag is called twice
        // once when a ghost role spawner is created and once when someone takes the ghost role

        if (TryFindValidTile(out var coords))
            comp.Coords = coords;
    }

    // Moffstation - Start - Rewrote this function to ensure coords are filled
    private void OnSelectLocation(Entity<AntagRandomSpawnComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords == null)
        {
            for (var i = 0; i < ent.Comp.Retries && ent.Comp.Coords == null; i++)
            {
                if (!TryFindValidTile(out var coords))
                    continue;

                ent.Comp.Coords = coords;
            }
        }

        if (ent.Comp.Coords != null)
            args.Coordinates.Add(_transform.ToMapCoordinates(ent.Comp.Coords.Value));
    }

    // Gives the atmos checks to make sure they arent being spawned in space and dying instantly
    private bool TryFindValidTile(out EntityCoordinates coords)
    {
        if (!TryFindRandomTile(out var tile, out var station, out var grid, out coords))
            return false;

        // Make sure they're on the largest grid, aka not spawning on the ATS
        if (_station.GetLargestGrid(station.Value) != grid)
            return false;

        if (Transform(grid).MapUid is not { } map || !_atmosphere.IsTileMixtureProbablySafe(grid, map, tile))
            return false;

        return true;
    }
    // Moffstation - End
}
