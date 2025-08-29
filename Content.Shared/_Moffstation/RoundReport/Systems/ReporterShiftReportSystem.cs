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

    private void OnPaperWrite(Entity<RoundReportComponent> ent, ref PaperInputTextMessage args)
    {
        if (TryComp(ent, out PaperComponent? paper))
        {
            ent.Comp.ReportBody = paper.Content;
        }
    }

}
