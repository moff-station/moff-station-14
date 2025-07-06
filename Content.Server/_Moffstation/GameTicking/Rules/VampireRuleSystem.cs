using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    /// <summary>
    /// Appends the round end text for the vampire role.
    /// </summary>
    protected override void AppendRoundEndText(EntityUid uid,
            VampireRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
    {
        var antags =_antag.GetAntagIdentifiers(uid);

        args.AddLine(antags.Count == 1
                     ? Loc.GetString("vampire-existing")
                     : Loc.GetString("vampires-existing", ("total", antags.Count)));

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("vampire-list-name-user", ("name", name), ("user", sessionData.UserName)));
            // todo: Add a count of how many people the vampire stole essence from.
        }
    }
}
