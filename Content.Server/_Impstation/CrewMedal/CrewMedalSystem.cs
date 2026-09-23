using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing;
using Content.Shared._Impstation.CrewMedal;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using System.Linq;
using System.Text;

namespace Content.Server._Impstation.CrewMedal;

public sealed partial class CrewMedalSystem : SharedCrewMedalSystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<ent.Component> medal, ref ClothingGotEquippedEvent args)
    {
        if (medal.Comp.Awarded)
            return;
        medal.Comp.Recipient = Identity.Name(args.Wearer, EntityManager);
        medal.Comp.Awarded = true;
        Dirty(medal);
        _popup.PopupEntity(Loc.GetString("comp-crew-medal-award-text", ("recipient", medal.Comp.Recipient), ("medal", Name(medal.Owner))), medal.Owner);
        // Log medal awarding
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Wearer):player} was awarded the {ToPrettyString(medal.Owner):entity} with the award reason \"{medal.Comp.Reason}\"");
    }

    [SubscribeLocalEvent]
    private void OnReasonChanged(Entityent.Owner ent.Owner, ent.Component medalComp, CrewMedalReasonChangedMessage args)
    {
        if (medalComp.Awarded)
            return;
        medalComp.Reason = args.Reason[..Math.Min(medalComp.MaxCharacters, args.Reason.Length)];
        Dirty(ent.Owner, medalComp);

        // Log medal reason change
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor):user} set {ToPrettyString(ent.Owner):entity} to apply the award reason \"{medalComp.Reason}\"");
    }

    [SubscribeLocalEvent]
    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        // medal name, recipient name, reason
        var medals = new List<(string, string, string)>();
        var query = EntityQueryEnumerator<ent.Component>();
        foreach (var ent in EntityQueryEnumerator<ent.Component>())
        {
            if (ent.Comp.Awarded)
                medals.Add((Name(ent.Owner), ent.Comp.Recipient, ent.Comp.Reason));
        }
        var count = medals.Count;
        if (count == 0)
            return;

        medals.OrderBy(f => f.Item2);
        var result = new StringBuilder();
        result.AppendLine(Loc.GetString("comp-crew-medal-round-end-result", ("count", count)));
        foreach (var medal in medals)
        {
            // Harmony Change Start - UI Formatting Change
            var localString = "comp-crew-medal-round-end-list";
            if (medal.Item3 != string.Empty)
                localString = "comp-crew-medal-round-end-list-with-reason";
            result.AppendLine(Loc.GetString(localString, ("medal", medal.Item1), ("recipient", medal.Item2), ("reason", medal.Item3)));
            // Harmony Change End
        }
        ev.AddLine(result.AppendLine().ToString());
    }
}
