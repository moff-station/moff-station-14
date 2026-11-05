using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Shared.Antag;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Rules;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed partial class ListeningOutpostRuleSystem : GameRuleSystem<ListeningOutpostRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;

    protected override void AppendRoundEndText(Entity<ListeningOutpostRuleComponent> rule, ref RoundEndTextAppendEvent args)
    {
        args.AddLine(Loc.GetString("lpo-existing"));
        args.AddLine(Loc.GetString("lpo-list-start"));

        var antags =_antag.GetAntagIdentifiers(rule.Owner);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("lpo-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
    }
}
