using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;

namespace Content.Shared._Moffstation.GaussFabricator;

public abstract partial class SharedGaussFabricatorSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<GaussFabricatorComponent>(GaussFabricatorUiKey.Key,
            subs =>
            {
                subs.Event<GaussFabricatorAdjustDrawRateMessage>(OnAdjustDrawRate);
                subs.Event<GaussFabricatorToggleMessage>(OnToggle);
            });
    }

    private void OnAdjustDrawRate(Entity<GaussFabricatorComponent> ent, ref GaussFabricatorAdjustDrawRateMessage args)
    {
        if (!float.IsFinite(args.Delta))
            return;

        ent.Comp.DrawRate = Math.Clamp(ent.Comp.DrawRate + args.Delta, ent.Comp.MinDrawRate, ent.Comp.MaxDrawRate);
        Dirty(ent);

        _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Actor):actor} set draw rate to {ent.Comp.DrawRate} W on {ToPrettyString(ent):target}");

        UpdateUi(ent);
    }

    private void OnToggle(Entity<GaussFabricatorComponent> ent, ref GaussFabricatorToggleMessage args)
    {
        if (ent.Comp.Enabled == args.On)
            return;

        ent.Comp.Enabled = args.On;
        Dirty(ent);

        _adminLog.Add(LogType.Action, $"{ToPrettyString(args.Actor):actor} toggled {ToPrettyString(ent):target} {(args.On ? "on" : "off")}");

        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<GaussFabricatorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(GaussFabricatorComponent)))
        {
            args.PushMarkup(Loc.GetString("gauss-fabricator-examine-draw-rate", ("rate", ent.Comp.DrawRate)));
            ExamineAtmosphere(ent, args);
        }
    }

    // Atmos is server only
    protected virtual void ExamineAtmosphere(Entity<GaussFabricatorComponent> ent, ExaminedEvent args)
    {
    }

    protected virtual void UpdateUi(Entity<GaussFabricatorComponent> ent)
    {
    }
}
