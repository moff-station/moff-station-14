using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server._Moffstation.Roles;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void AppendRoundEndText(EntityUid uid,
            VampireRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
    {
        var antags =_antag.GetAntagIdentifiers(uid);

        if (antags.Count == 1)
            args.AddLine(Loc.GetString("vampire-existing"));
        else
            args.AddLine(Loc.GetString("vampires-existing", ("total", antags.Count)));

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("vampire-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }
    }
}
