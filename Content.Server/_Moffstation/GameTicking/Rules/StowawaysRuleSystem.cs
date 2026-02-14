using System.Linq;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server._Moffstation.Roles;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Prototypes;
using Content.Server.Antag.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class StowawaysRuleSystem : GameRuleSystem<StowawaysRuleComponent>
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    public override void Initialize()
    {
        Log.Warning($"Stowaway init!");
        base.Initialize();

        SubscribeLocalEvent<StowawayRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(RulePlayerSpawningEvent args)
    {
        Log.Warning($"Stowaway player spawning!");
        /// <summary>
        ///     Pool of players to be spawned.
        ///     If you want to handle a specific player being spawned, remove it from this list and do what you need.
        /// </summary>
        /// <remarks>If you spawn a player by yourself from this event, don't forget to call <see cref="GameTicker.PlayerJoinGame"/> on them.</remarks>
        var pool = args.PlayerPool;
        int numStowaways = 3; 
        var stations = _station.GetStations();
        int stowawaysPerStation = (int)Math.Ceiling((double)numStowaways / stations.Count);
        ProtoId<JobPrototype> job = "Stowaway";

        // This rule adds Stowaways to the roundstart jobs (and ONLY roundstart!)(?)
        foreach (var station in stations)
        {
            if (!_stationJobs.TryAdjustRoundstartJobSlot(station, job, stowawaysPerStation, true))
            {
                Log.Warning($"Stowaway failed: unable to add {job} to {station}!");
                continue;
            }

            // TODO: also spawn spawners at rando randoms
        }
    }

    private void OnGetBriefing(Entity<StowawayRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append("hi you're a stowaway");
    }
}
