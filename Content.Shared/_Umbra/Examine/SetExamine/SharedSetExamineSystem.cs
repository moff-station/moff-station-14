using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs;

namespace Content.Shared._Umbra.Examine;

public abstract class SharedSetExamineSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetExamineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SetExamineComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SetExamineComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<SetExamineComponent> ent, ref MapInitEvent ev)
    {
        if (_actions.AddAction(ent, ref ent.Comp.Action, out var action, ent.Comp.ActionPrototype))
            action.EntityIcon = ent;
    }

    private void OnExamine(Entity<SetExamineComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (comp.ExamineText.Trim() == string.Empty)
            return;

        using (args.PushGroup(nameof(SetExamineComponent)))
        {
            var ExamineText = Loc.GetString("set-examine-examined", ("ent", ent), ("ExamineText", comp.ExamineText));
            args.PushMarkup(ExamineText, -5);
        }
    }

    private void OnMobStateChanged(Entity<SetExamineComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        ent.Comp.ExamineText = string.Empty; // reset the ExamineText on death/crit
        Dirty(ent);
    }
}

public sealed partial class SetExamineActionEvent : InstantActionEvent;
