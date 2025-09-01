using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class ListeningOutpostRuleSystem : GameRuleSystem<ListeningOutpostRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

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
}
