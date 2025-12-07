using System.Linq;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.GameTicking.Rules;

/// <summary>
/// This system listens for the player spawning event, and attempts to spawn them away from other players
/// </summary>
public sealed class AvoidantSpawningSystem : GameRuleSystem<AvoidantSpawningComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: [typeof(SpawnPointSystem)]);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        var gameRules = EntityQueryEnumerator<AvoidantSpawningComponent, GameRuleComponent>();
        while (gameRules.MoveNext(out var gameRuleUid, out var avoidantComp, out var gameruleComp))
        {
            if (!GameTicker.IsGameRuleActive(gameRuleUid, gameruleComp))
                continue;

            args.SpawnResult = SpawnPlayerMob((gameRuleUid, avoidantComp), args);
            return;
        }
    }

    private EntityUid? SpawnPlayerMob(Entity<Components.AvoidantSpawningComponent> gameRule, PlayerSpawningEvent args)
    {
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();
        var fallbackPositions = new List<EntityCoordinates>();

        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            if (BlacklistInRange(gameRule, (uid, xform)))
                possiblePositions.Add(xform.Coordinates);
            else
                fallbackPositions.Add(xform.Coordinates);
        }

        if (possiblePositions.Count == 0 && fallbackPositions.Count == 0)
            return null;

        var spawnLoc = possiblePositions.Count > 0
            ? _random.Pick(possiblePositions)
            : _random.Pick(fallbackPositions);

        return _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }

    private bool BlacklistInRange(Entity<Components.AvoidantSpawningComponent> gameRule, Entity<TransformComponent> spawnPoint)
    {
        var entities = new HashSet<EntityUid>();
        _entityLookup.GetEntitiesInRange(spawnPoint.Comp.Coordinates, gameRule.Comp.Range, entities);
        return entities.Select(e => _whitelist.IsWhitelistPass(gameRule.Comp.Blacklist, e)).Any();
    }
}
