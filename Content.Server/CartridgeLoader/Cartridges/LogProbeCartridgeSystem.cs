using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared._Moffstation.NanoChat; // CD
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using System.Text;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared.Audio;
using Robust.Shared.Audio; // Moffstation - Nanochat Rewrite
using Robust.Shared.Random; // Moffstation - Nanochat Rewrite
namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem : EntitySystem // CD - Made partial
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IRobustRandom _random = default!; // Moffstation - Nanochat Rewrite

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeMessageEvent>(OnMessage);
        SubscribeLocalEvent<NanoChatRecipientUpdatedEvent>(OnRecipientUpdated); // Moffstation - Nanochat Rewrite
        SubscribeLocalEvent<NanoChatMessageReceivedEvent>(OnMessageReceived); // Moffstation - Nanochat Rewrite
    }

    /// <summary>
    /// The <see cref="CartridgeAfterInteractEvent" /> gets relayed to this system if the cartridge loader is running
    /// the LogProbe program and someone clicks on something with it. <br/>
    /// <br/>
    /// Updates the program's list of logs with those from the device.
    /// </summary>
    private void AfterInteract(Entity<LogProbeCartridgeComponent> ent, ref CartridgeAfterInteractEvent args)
    {
        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || args.InteractEvent.Target is not { } target)
            return;

        // CD begin - Add NanoChat card scanning
        if (TryComp<NanoChatCardComponent>(target, out var nanoChatCard))
        {
            ScanNanoChatCard(ent, args, target, nanoChatCard);
            args.InteractEvent.Handled = true;
            return;
        }
        // CD end

        if (!TryComp(target, out AccessReaderComponent? accessReaderComponent))
            return;

        //Play scanning sound with slightly randomized pitch
        _audio.PlayEntity(ent.Comp.SoundScan, args.InteractEvent.User, target);
        _popup.PopupCursor(Loc.GetString("log-probe-scan", ("device", target)), args.InteractEvent.User);

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

        UpdateUiState(ent, args.Loader);
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
        if (args is LogProbePrintMessage cast)
            PrintLogs(ent, cast.User);
    }

    private void PrintLogs(Entity<LogProbeCartridgeComponent> ent, EntityUid user)
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

    public void UpdateUiState(Entity<LogProbeCartridgeComponent> ent, EntityUid loaderUid)
    {
        var state = new LogProbeUiState(ent.Comp.EntityName, ent.Comp.PulledAccessLogs, ent.Comp.ScannedNanoChatData); // CD - NanoChat support
        _cartridge.UpdateCartridgeUiState(loaderUid, state);
    }
    //Moffstation - Begin - Nanochat Rewrite
     private void OnRecipientUpdated(ref NanoChatRecipientUpdatedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData == null || GetEntity(probe.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                new Dictionary<uint, NanoChatRecipient>(card.Recipients),
                probe.ScannedNanoChatData.Value.Messages,
                card.Number,
                GetNetEntity(args.CardUid));

            if (cartridge.LoaderUid != null)
                UpdateUiState((uid, probe), cartridge.LoaderUid.Value);
        }
    }

    private void OnMessageReceived(ref NanoChatMessageReceivedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData == null || GetEntity(probe.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                probe.ScannedNanoChatData.Value.Recipients,
                new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
                card.Number,
                GetNetEntity(args.CardUid));

            if (cartridge.LoaderUid != null)
                UpdateUiState((uid, probe), cartridge.LoaderUid.Value);
        }
    }

    public void ScanNanoChatCard(Entity<LogProbeCartridgeComponent> ent,
        CartridgeAfterInteractEvent args,
        EntityUid target,
        NanoChatCardComponent card)
    {
        _audio.PlayEntity(ent.Comp.SoundScan,
            args.InteractEvent.User,
            target,
            AudioParams.Default.WithVariation(0.25f));

           ;

        _popup.PopupCursor(Loc.GetString("log-probe-scan-nanochat", ("card", target)), args.InteractEvent.User);
        ;
        ent.Comp.PulledAccessLogs.Clear();
        ent.Comp.ScannedNanoChatData = new NanoChatData(
            new Dictionary<uint, NanoChatRecipient>(card.Recipients),
            new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
            card.Number,
            GetNetEntity(target)
        );

        UpdateUiState(ent, args.Loader);
    }
    //Moffstation - End
}
