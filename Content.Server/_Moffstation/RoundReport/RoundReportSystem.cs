using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared._Moffstation.RoundReport.Components;
using Content.Shared.Alert;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.RoundReport;

public sealed class RoundReportSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(AppendRoundEndText);
    }

    private void AppendRoundEndText(RoundEndTextAppendEvent args)
    {
        var query = EntityQueryEnumerator<RoundReportComponent>();
        while (query.MoveNext(out var uid, out var roundReport))
        {
            if (roundReport.ReportHeader != "" || roundReport.ReportBody != "")
            {
                if (!Loc.TryGetString(roundReport.ReportHeader, out var header))
                    header = roundReport.ReportHeader;
                args.AddLineWrapping($"[color={roundReport.HeaderColor}]{header}[/color]", roundReport.LineWidth);
                args.AddLineWrapping($"[color={roundReport.BodyColor}]{roundReport.ReportBody}[/color]", roundReport.LineWidth);
                args.AddLine("");
            }
        }
    }
}
