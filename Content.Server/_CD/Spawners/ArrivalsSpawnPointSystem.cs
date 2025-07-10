using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CD.Spawners;

public sealed class ArrivalsSpawnPointSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        // Check if cvar disables this feature
        if (!_cfgManager.GetCVar(CCVars.StartAtArrivals))
            return;

        // If it's a latejoin and past the forced arrivals timer, allow choosing cryosleep
        if (args is { LateJoin: true, Profile.SpawnPriority: not SpawnPriorityPreference.Arrivals } &&
            _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan) > TimeSpan.FromMinutes(_cfgManager.GetCVar(CCVars.SpawnPreferenceDelay)))
            return;

        // Ensure they have a job, so that we won't end up making mobs spawn on arrivals.
        if (args.JobId == null)
            return;

        // Get job, skip everything else if it ignores arrivals
        if (!_prototypeManager.TryIndex<JobPrototype>(args.JobId, out var job))
            return;
        if (job.IgnoreArrivals)
            return;

        var spawnsList = new List<Entity<ArrivalsSpawnPointComponent>>();
        var query = EntityQueryEnumerator<ArrivalsSpawnPointComponent>();

        // Get them in a list so we can do list things
        while (query.MoveNext(out var spawnUid, out var spawnPoint))
        {
            spawnsList.Add((spawnUid, spawnPoint));
        }

        // Return if there's no spawns that exist
        if (spawnsList.Count == 0)
            return;

        // Make sure map is unpaused
        if (_mapSystem.IsPaused(Transform(spawnsList.First()).MapID))
            _mapSystem.SetPaused(Transform(spawnsList.First()).MapID, false);

        // Make it random just in case
        _random.Shuffle(spawnsList);

        // Job spawns first
        foreach (var spawn in spawnsList)
        {
            foreach (var jobId in spawn.Comp.JobIds)
            {
                if (job.ID == jobId)
                {
                    _transform.SetCoordinates(args.Mob, Transform(spawn.Owner).Coordinates);
                    return;
                }
            }
        }

        // Random spawn next, ensure that it's NOT a jobspawn
        foreach (var spawn in spawnsList)
        {
            if (spawn.Comp.JobIds.Count == 0)
            {
                _transform.SetCoordinates(args.Mob, Transform(spawn.Owner).Coordinates);
                return;
            }
        }
    }
}
