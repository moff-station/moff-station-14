using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared._CD.NanoChat; // CD
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using System.Text;
using Content.Shared.Interaction;
using Content.Server._Moffstation.LogProbe;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.LogProbe;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem : EntitySystem // CD - Made partial
{
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PaperSystem _paper = default!;

    [Dependency] private SharedUserInterfaceSystem _userInterface = default!; // Moffstation

    public override void Initialize()
    {
        base.Initialize();

        InitializeNanoChat(); // CD
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeMessageEvent>(OnMessage);

        // Moffstation - Begin - Handle events for non-PDA version
        SubscribeLocalEvent<LogProbeComponent, AfterInteractEvent>(AfterInteract);

        Subs.BuiEvents<LogProbeComponent>(LogProbeUiKey.Key,
            subs =>
            {
                subs.Event<LogProbePrintBuiMessage>(OnPrintMessage);
            });
        // Moffstation - End
    }

    // Moffstation - Begin - Handle events for non-PDA version
    private void AfterInteract(Entity<LogProbeComponent> ent, ref AfterInteractEvent args)
    {
        AfterInteract(ent, args, () => UpdateUiState(ent));
    }

    private void OnPrintMessage(Entity<LogProbeComponent> ent, ref LogProbePrintBuiMessage msg)
    {
        PrintLogs(ent, msg.Actor);
    }

    private void UpdateUiState(Entity<LogProbeComponent> ent)
    {
        var state = new LogProbeUiState(ent.Comp.EntityName, ent.Comp.PulledAccessLogs);
        _userInterface.SetUiState(ent.Owner, LogProbeUiKey.Key, state);
    }
    // Moffstation - End

    /// <summary>
    /// The <see cref="CartridgeAfterInteractEvent" /> gets relayed to this system if the cartridge loader is running
    /// the LogProbe program and someone clicks on something with it. <br/>
    /// <br/>
    /// Updates the program's list of logs with those from the device.
    /// </summary>
    private void AfterInteract(Entity<LogProbeCartridgeComponent> ent, ref CartridgeAfterInteractEvent args)
    {
        // Moffstation - Begin - Split the component to be reusable
        var loader = args.Loader;
        AfterInteract(ent, args.InteractEvent, () => UpdateUiState(ent, loader));
        // Moffstation - End

        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || args.InteractEvent.Target is not { } target)
            return;


    }

    private void AfterInteract<T>(Entity<T> ent, AfterInteractEvent args, Action updateState) // Moffstation - Split the component to be reusable
        where T : BaseLogProbeComponent
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        // CD begin - Add NanoChat card scanning
        if (TryComp<NanoChatCardComponent>(target, out var nanoChatCard))
        {
            ScanNanoChatCard(ent, args.User, (target, nanoChatCard));
            updateState();
            args.Handled = true;
            return;
        }
        // CD end

        if (!TryComp(target, out AccessReaderComponent? accessReaderComponent))
            return;

        //Play scanning sound with slightly randomized pitch
        _audio.PlayEntity(ent.Comp.SoundScan, args.User, target); // Moffstation - Split
        _popup.PopupCursor(Loc.GetString("log-probe-scan", ("device", target)), args.User); // Moffstation - Split

        ent.Comp.EntityName = Name(target);
        ent.Comp.PulledAccessLogs.Clear();
        ent.Comp.ScannedNanoChatData = null; // CD - Clear any previous NanoChat data

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

        updateState(); // Moffstation - Split
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(Entity<LogProbeCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void OnMessage(Entity<LogProbeCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is Shared.CartridgeLoader.Cartridges.LogProbePrintMessage cast)
            PrintLogs(ent, cast.User);
    }


    private void PrintLogs<T>(Entity<T> ent, EntityUid user) // Moffstation - Split the component to be reusable
        where T : BaseLogProbeComponent
    {
        if (string.IsNullOrEmpty(ent.Comp.EntityName))
            return;

        if (_timing.CurTime < ent.Comp.NextPrintAllowed)
            return;

        ent.Comp.NextPrintAllowed = _timing.CurTime + ent.Comp.PrintCooldown;

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

    private void UpdateUiState(Entity<LogProbeCartridgeComponent> ent, EntityUid loaderUid)
    {
        var state = new LogProbeUiState(ent.Comp.EntityName, ent.Comp.PulledAccessLogs, ent.Comp.ScannedNanoChatData); // CD - NanoChat support
        _cartridge.UpdateCartridgeUiState(loaderUid, state);
    }

    // Moffstation - Begin
    /// <summary>
    /// Returns an enumerable over all <see cref="BaseLogProbeComponent"/> and a function which can be called
    /// to cause the probe's associated UI to be updated.
    /// </summary>
    private IEnumerable<(Entity<BaseLogProbeComponent> ent, Action updateUi)> AllLogProbes()
    {
        using (var queryEnumerator = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>())
        {
            foreach (var entity in queryEnumerator.AsEnumerable())
            {
                yield return (
                    (entity, entity),
                    () =>
                    {
                        if (entity.Comp2.LoaderUid is { } loader)
                            UpdateUiState(entity, loader);
                    }
                );
            }
        }

        using (var entityQueryEnumerator = EntityQueryEnumerator<LogProbeComponent>())
        {
            foreach (var entity in entityQueryEnumerator.AsEnumerable())
            {
                yield return ((entity, entity), () => UpdateUiState(entity));
            }
        }
    }
    // Moffstation - End
}
