using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class PiratesRuleSystem : GameRuleSystem<PiratesRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

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
}
