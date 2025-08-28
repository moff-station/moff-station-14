using System.Linq;
using Content.Server._Moffstation.Spawning.Components;
using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Spawning.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class AvoidantSpawnPointSystem : EntitySystem
{

    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: [typeof(SpawnPointSystem)]);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        var points = EntityQueryEnumerator<AvoidantSpawnPointComponent, SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (points.MoveNext(out var uid, out var avoidantPoint, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            if (_gameTicker.RunLevel == GameRunLevel.InRound &&
                 spawnPoint.SpawnType == SpawnPointType.LateJoin
                || _gameTicker.RunLevel != GameRunLevel.InRound &&
                spawnPoint.SpawnType == SpawnPointType.Job &&
                (args.Job == null || spawnPoint.Job == args.Job))
            {

                possiblePositions.Add(xform.Coordinates);
            }
        }

        _random.Shuffle(possiblePositions);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }

    private bool BlacklistInRange(Entity<AvoidantSpawnPointComponent, TransformComponent> entity)
    {
        var entities = new HashSet<EntityUid>();
        _entityLookup.GetEntitiesInRange(entity.Comp2.Coordinates, entity.Comp1.Range, entities);
        return entities.Select(e => _whitelist.IsBlacklistPass(entity.Comp1.Blacklist, e)).Any();
    }
}
