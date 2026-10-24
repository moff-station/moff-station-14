using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;

namespace Content.Shared._Moffstation.GaussFabricator;

public abstract partial class GaussFabricatorSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<GaussFabricatorComponent>(GaussFabricatorUiKey.Key,
            subs =>
            {
                subs.Event<GaussFabricatorSetDrawMessage>(OnSetDraw);
                subs.Event<GaussFabricatorToggleMessage>(OnToggle);
            });
    }

    private void OnSetDraw(Entity<GaussFabricatorComponent> ent, ref GaussFabricatorSetDrawMessage args)
    {
        if (!args.Draw.IsValid())
            return;

        ent.Comp.CurrentDraw = Math.Clamp(args.Draw, ent.Comp.MinDraw, ent.Comp.MaxDraw);
        DirtyField(ent.AsNullable(), nameof(GaussFabricatorComponent.CurrentDraw));

        _adminLog.Add(LogType.Action, LogImpact.Low, $"{args.Actor:actor} set draw rate to {ent.Comp.CurrentDraw} W on {ent.Owner:target}");

        SyncPower(ent);
        UpdateUi(ent);
    }

    private void OnToggle(Entity<GaussFabricatorComponent> ent, ref GaussFabricatorToggleMessage args)
    {
        if (ent.Comp.Enabled == args.On)
            return;

        ent.Comp.Enabled = args.On;
        DirtyField(ent.AsNullable(), nameof(GaussFabricatorComponent.Enabled));

        _adminLog.Add(LogType.Action, LogImpact.Low, $"{args.Actor:actor} toggled {ent.Owner:target} {(args.On ? "on" : "off")}");

        SyncPower(ent);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<GaussFabricatorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(GaussFabricatorComponent)))
        {
            args.PushMarkup(Loc.GetString("gauss-fabricator-examine-draw-rate", ("rate", ent.Comp.CurrentDraw)));
            ExamineAtmosphere(ent, args);
        }
    }

    // Atmos is server only
    protected abstract void ExamineAtmosphere(Entity<GaussFabricatorComponent> ent, ExaminedEvent args);

    // The power network battery is server only
    protected abstract void SyncPower(Entity<GaussFabricatorComponent> ent);

    protected abstract void UpdateUi(Entity<GaussFabricatorComponent> ent);
}
