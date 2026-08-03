using Content.Server.Administration;
using Content.Shared._Umbra.Examine.SetExamine;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Server._Umbra.Examine;

public sealed partial class SetExamineSystem : SharedSetExamineSystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetExamineComponent, SetExamineActionEvent>(OnSetExamineAction);
    }

    private void OnSetExamineAction(Entity<SetExamineComponent> ent, ref SetExamineActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        var setExaminePrompt = Loc.GetString("set-examine-dialog", ("ent", ent));

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("set-examine-title"), setExaminePrompt,
            (string ExamineText) =>
            {
                _adminLog.Add(LogType.Action, $"{ToPrettyString(ent)} set their examine text to: {ExamineText}");
                ent.Comp.ExamineText = ExamineText;
                Dirty(ent);
            });


        args.Handled = true;
    }
}
