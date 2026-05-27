using System.Text;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.LogProbe;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class LogProbeSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _time = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LogProbeComponent, AfterInteractEvent>(OnAfterInteract);

        Subs.BuiEvents<LogProbeComponent>(LogProbeUiKey.Key,
            subs =>
            {
                subs.Event<LogProbePrintMessage>(OnPrintMessage);
            });
    }

    private void OnAfterInteract(Entity<LogProbeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp(target, out AccessReaderComponent? accessReaderComponent))
            return;

        //Play scanning sound with slightly randomized pitch
        _audio.PlayPredicted(ent.Comp.SoundScan, args.User, target);
        _popup.PopupCursor(Loc.GetString("log-probe-scan", ("device", target)), args.User);

        ent.Comp.EntityName = Name(target);
        ent.Comp.PulledAccessLogs.Clear();

        foreach (var accessRecord in accessReaderComponent.AccessLog)
        {
            var log = new PulledAccessLog(
                accessRecord.AccessTime,
                accessRecord.Accessor
            );

            ent.Comp.PulledAccessLogs.Add(log);
        }

        // Reverse the list so the oldest is at the bottom
        ent.Comp.PulledAccessLogs.Reverse();

        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<LogProbeComponent> ent)
    {
        var state = new LogProbeUiState(ent.Comp.EntityName, ent.Comp.PulledAccessLogs);
        _userInterface.SetUiState(ent.Owner, LogProbeUiKey.Key, state);
    }

    private void OnPrintMessage(Entity<LogProbeComponent> ent, ref LogProbePrintMessage args)
    {
        var user = args.Actor;

        if (string.IsNullOrEmpty(ent.Comp.EntityName))
            return;

        if (_time.CurTime < ent.Comp.NextPrintAllowed)
            return;

        ent.Comp.NextPrintAllowed = _time.CurTime + ent.Comp.PrintCooldown;

        var paper = Spawn(ent.Comp.PaperPrototype, _transform.GetMapCoordinates(user));
        _label.Label(paper, ent.Comp.EntityName); // label it for easy identification

        _audio.PlayEntity(ent.Comp.PrintSound, user, paper);
        _hands.PickupOrDrop(user, paper, checkActionBlocker: false);

        // generate the actual printout text
        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("log-probe-printout-device", ("name", ent.Comp.EntityName)));
        builder.AppendLine(Loc.GetString("log-probe-printout-header"));
        var number = 1;
        foreach (var log in ent.Comp.PulledAccessLogs)
        {
            var time = TimeSpan.FromSeconds(Math.Truncate(log.Time.TotalSeconds)).ToString();
            builder.AppendLine(Loc.GetString("log-probe-printout-entry", ("number", number), ("time", time), ("accessor", log.Accessor)));
            number++;
        }

        var paperComp = Comp<PaperComponent>(paper);
        _paper.SetContent((paper, paperComp), builder.ToString());

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):user} printed out LogProbe logs ({paper}) of {ent.Comp.EntityName}");
    }
}
