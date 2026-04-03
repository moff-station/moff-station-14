using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Robust.Shared.RichText;
using Robust.Shared.Utility;

namespace Content.Shared._Umbra.Examine.SetExamine;

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
            _actions.SetEntityIcon((ent.Comp.Action.Value, action), ent);
    }

    private void OnExamine(Entity<SetExamineComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;

        if (comp.ExamineText.Trim() == string.Empty)
            return;

        using (args.PushGroup(nameof(SetExamineComponent)))
        {
            var examineText = Loc.GetString("set-examine-examined", ("ent", ent), ("ExamineText", FormattedMessage.EscapeText(comp.ExamineText)));
            args.PushMarkup(examineText, -5);
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
