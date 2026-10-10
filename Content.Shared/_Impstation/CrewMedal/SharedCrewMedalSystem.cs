using Content.Shared.Examine;
using Robust.Shared.Utility;
namespace Content.Shared._Impstation.CrewMedal;

public class SharedCrewMedalSystemImpl : SharedCrewMedalSystem // Moff - change to not require partial, abstract, sealed, etc
{
    [SubscribeLocalEvent]
    private void OnExamined(Entity<CrewMedalComponent> medal, ref ExaminedEvent args)
    {
        if (!medal.Comp.Awarded)
            return;

        // Harmony Change Start - UI Formatting Change
        var localAwardString = medal.Comp.Reason == String.Empty ? "comp-crew-medal-inspection-text" : "comp-crew-medal-inspection-text-with-reason";
        var str = Loc.GetString(localAwardString, ("recipient", medal.Comp.Recipient), ("reason", FormattedMessage.EscapeText(medal.Comp.Reason)));
        // Harmony Change End
        args.PushMarkup(str);
    }
}
