using System.Linq;
using Content.Shared._Moffstation.RoundReport.Components;
using Content.Shared.Paper;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared._Moffstation.RoundReport.Systems;

public sealed class ReporterShiftReportSystem : EntitySystem
{

    // [Dependency] private readonly PaperSystem _paperSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundReportComponent, PaperInputTextMessage>(OnPaperWrite);
    }

    private void OnPaperWrite(EntityUid uid, RoundReportComponent component, ref PaperInputTextMessage args)
    {
        if (TryComp(uid, out PaperComponent? paper))
        {
            component.ReportBody = paper.Content;
        }
    }

}
