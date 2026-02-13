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

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class StowawaysRuleSystem : GameRuleSystem<StowawaysRuleComponent>
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<StowawayRoleComponent, GetBriefingEvent>(OnGetBriefing);
        SubscribeLocalEvent<StowawaysRuleComponent, RulePlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(Entity<StowawaysRuleComponent> ent, ref RulePlayerSpawningEvent args)
    {
        /// <summary>
        ///     Pool of players to be spawned.
        ///     If you want to handle a specific player being spawned, remove it from this list and do what you need.
        /// </summary>
        /// <remarks>If you spawn a player by yourself from this event, don't forget to call <see cref="GameTicker.PlayerJoinGame"/> on them.</remarks>
        var pool = args.PlayerPool;
        int numStowaways = (int)Math.Ceiling(pool.Count * ent.Comp.PlayerRatio);
        var stations = _station.GetStations();
        int stowawaysPerStation = (int)Math.Ceiling((double)numStowaways / stations.Count);

        // This rule adds Stowaways to the roundstart jobs (and ONLY roundstart!)(?)
        foreach (var station in stations)
        {
            if (!_stationJobs.TryAdjustJobSlot(station, ent.Comp.Job, stowawaysPerStation, true))
            {
                Log.Warning($"Stowaway failed: unable to add {ent.Comp.Job} to {station}!");
                continue;
            }

            // TODO: also spawn spawners at rando randoms
        }
    }

    /*
    private void OnGetBriefing(Entity<StowawayRoleComponent> role, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("pirate-briefing"));
    }*/
}
