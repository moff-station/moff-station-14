using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.Chat;
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
    [Dependency] private readonly IChatManager _chat = default!;

    // The chance that there will be player who spawn on station at all
    // as in, if this rolls people will spawn on station, and if it doesn't, nobody will spawn on station
    private const float NormalSpawnChance = 0.6f;
    // If people are to spawn normally, how many player max should spawn on station?
    private const int NormalSpawnMaxLimit = 5;

    private int _normalSpawnLimit;
    private int _normalSpawnCount;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnRoundStart(RoundStartingEvent args)
    {
        _normalSpawnCount = 0;
        _normalSpawnLimit = _random.Prob(NormalSpawnChance) ? _random.Next(1, NormalSpawnMaxLimit) : 0;
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        // Check if cvar disables this feature
        if (!_cfgManager.GetCVar(MoffCCVars.StartAtArrivals))
            return;

        // This should be random... unless people are spawned in a certain order? who knows
        if (_normalSpawnCount < _normalSpawnLimit)
        {
            _normalSpawnCount++;
            var message = Loc.GetString("opening-shift-greeting");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            _chat.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, args.Player.Channel, Color.CornflowerBlue);
            return;
        }

        // If it's a latejoin and past the forced arrivals timer, allow choosing cryosleep
        if (args is { LateJoin: true, Profile.SpawnPriority: not SpawnPriorityPreference.Arrivals } &&
            _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan) > TimeSpan.FromMinutes(_cfgManager.GetCVar(MoffCCVars.SpawnPreferenceDelay)))
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
