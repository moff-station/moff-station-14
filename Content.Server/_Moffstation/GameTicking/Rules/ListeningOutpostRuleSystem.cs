using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared._Moffstation.ListeningOutpost.Components;
using Content.Shared.Cargo.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed partial class ListeningOutpostRuleSystem : GameRuleSystem<ListeningOutpostRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ListeningOutpostRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ListeningOutpostRuleComponent listeningOutpostRuleComponent,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        args.AddLine(Loc.GetString("lpo-existing"));
        args.AddLine(Loc.GetString("lpo-list-start"));

        var antags =_antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("lpo-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
    }

    private void OnRuleLoadedGrids(Entity<ListeningOutpostRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        // Check each added grid to see if it's a listening outpost base.
        foreach (var grid in args.Grids)
        {
            if (!TryComp<ListeningOutpostBaseComponent>(grid, out var baseComp))
                continue;

            baseComp.AssociatedRule = ent;

            // Converts the listening outpost base into a station, giving it a functional cargo system
            ent.Comp.AssociatedStation = _station.InitializeNewStation(ent.Comp.StationConfig, [grid]);

            // Give the listening outpost station component a reference to this rule for later reference
            if (!TryComp<ListeningOutpostStationComponent>(ent.Comp.AssociatedStation, out var stationComp))
                continue;
            stationComp.AssociatedRule = GetNetEntity(ent.Owner);

            // Turns the listening outpost base into a trade station, so that its buy/sell pads are functional
            //EnsureComp<TradeStationComponent>(grid);
            //Dirty(grid, baseComp);
        }
    }
}
