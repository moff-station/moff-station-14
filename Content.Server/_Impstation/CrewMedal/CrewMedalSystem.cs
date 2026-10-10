using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Clothing;
using Content.Shared._Impstation.CrewMedal;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using System.Linq;
using System.Text;
using Robust.Shared.Utility; // Moff - add missing references

namespace Content.Server._Impstation.CrewMedal;

public sealed partial class CrewMedalSystem : SharedCrewMedalSystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<CrewMedalComponent> medal, ref ClothingGotEquippedEvent args)
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
    private void OnReasonChanged(Entity<CrewMedalComponent> medal, ref CrewMedalReasonChangedMessage args)
    {
        if (medal.Comp.Awarded)
            return;
        medal.Comp.Reason = args.Reason[..Math.Min(medal.Comp.MaxCharacters, args.Reason.Length)];
        Dirty(medal, medal.Comp);

        // Log medal reason change
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Actor):user} set {ToPrettyString(medal):entity} to apply the award reason \"{medal.Comp.Reason}\"");
    }

    [SubscribeLocalEvent]
    private void OnRoundEndText(ref RoundEndTextAppendEvent ev)
    {
        // medal name, recipient name, reason
        var medals = new List<(string Name, string Recipient, string Reason)>();
        //var query = EntityQueryEnumerator<ent.Component>(); Moff - remove line
        foreach (var ent in EntityQueryEnumerator<CrewMedalComponent>())
        {
            if (ent.Comp.Awarded)
                medals.Add((Name(ent.Owner), ent.Comp.Recipient, FormattedMessage.EscapeText(ent.Comp.Reason)));
        }
        var count = medals.Count;
        if (count == 0)
            return;

        var result = new StringBuilder();
        result.AppendLine(Loc.GetString("comp-crew-medal-round-end-result", ("count", count)));
        foreach (var medal in medals.OrderBy(f => f.Recipient))
        {
            // Harmony Change Start - UI Formatting Change
            var localString = "comp-crew-medal-round-end-list";
            if (medal.Reason != string.Empty)
                localString = "comp-crew-medal-round-end-list-with-reason";
            result.AppendLine(Loc.GetString(localString, ("medal", medal.Name), ("recipient", medal.Recipient), ("reason", medal.Reason)));
            // Harmony Change End
        }
        ev.AddLine(result.AppendLine().ToString());
    }
}
