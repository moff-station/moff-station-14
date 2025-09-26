using Content.Server._Moffstation.GameTicking.Rules;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class ClockworkCultRuleSystem : GameRuleSystem<ClockworkCultRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    /// <summary>
    /// Appends the round end text for the vamp-CULTIST role.
    /// </summary>
    protected override void AppendRoundEndText(
            EntityUid uid,
            ClockworkCultRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
    {
        var antags =_antag.GetAntagIdentifiers(uid);

        args.AddLine(Loc.GetString("clockcult-existing"));

        args.AddLine(Loc.GetString("clockcult-list-start"));

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("clockcult-list-name-user", ("name", name), ("user", sessionData.UserName)));
            // todo: Add a count of how many people the vampire stole essence from.
        }
    }
}