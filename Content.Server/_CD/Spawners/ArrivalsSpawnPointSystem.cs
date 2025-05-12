using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CD.Spawners;

public sealed class ArrivalsSpawnPointSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
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

        // Random spawn next
        _transform.SetCoordinates(args.Mob, Transform(spawnsList.First().Owner).Coordinates);
    }
}
