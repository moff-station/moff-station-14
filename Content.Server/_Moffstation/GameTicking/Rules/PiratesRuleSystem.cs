using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.Cargo.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.Pirate.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class PiratesRuleSystem : GameRuleSystem<PiratesRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PiratesRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
    }
    protected override void AppendRoundEndText(EntityUid uid,
        PiratesRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        args.AddLine(Loc.GetString("pirates-existing"));
        args.AddLine(Loc.GetString("pirate-list-start"));

        var antags =_antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("pirate-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
    }

    private void OnRuleLoadedGrids(Entity<PiratesRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        // Check each Pirate shuttle
        var query = EntityQueryEnumerator<PirateShuttleComponent>();
        while (query.MoveNext(out var uid, out var shuttle))
        {
            // Check if the shuttle's mapID is the one that just got loaded for this rule
            if (Transform(uid).MapID == args.Map)
            {
                shuttle.AssociatedRule = ent;

                // Converts the pirate shuttle into a station, giving it a functional cargo system
                _station.InitializeNewStation(ent.Comp.StationConfig, [uid]);

                //Turns the pirate shuttle into a trade station, so that it's buy/sell pads are functional
                EnsureComp<TradeStationComponent>(uid);
                Dirty(uid, shuttle);

                break;
            }
        }
    }
}
