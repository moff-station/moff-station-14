using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Shared._Moffstation.BladeServer;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.CartridgeLoader;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

// "Chitter P.I." - point a PDA running this cartridge at a Chitter server (racked or otherwise) to pull a one-shot
// snapshot of every chat it holds, live and archived. Unlike the normal Chitter cartridge, this never talks back to
// the server after the scan - re-scan to refresh.
public sealed partial class ChitterPiCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly SoundSpecifier ScanFinishSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChitterPiCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ChitterPiCartridgeComponent, CartridgeRelayedEvent<AfterInteractEvent>>(OnAfterInteract);
        SubscribeLocalEvent<ChitterPiCartridgeComponent, ChitterPiScanDoAfterEvent>(OnDoAfter);
    }

    private void OnUiReady(Entity<ChitterPiCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent, args.Loader);
    }

    private void OnAfterInteract(Entity<ChitterPiCartridgeComponent> ent, ref CartridgeRelayedEvent<AfterInteractEvent> args)
    {
        if (args.Args.Handled || !args.Args.CanReach || args.Args.Target is not { } target)
            return;

        if (!TryFindChitterServer(target, out var serverEnt))
            return;

        args.Args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Args.User, ent.Comp.ScanDelay, new ChitterPiScanDoAfterEvent(), ent.Owner, target: serverEnt.Owner, used: args.Loader)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    // A Chitter server is either a loose ChitterServer entity, or - the common case - a ChitterBladeServer racked
    // inside a BladeServerRack, in which case the player clicks the rack itself rather than the (contained) blade.
    private bool TryFindChitterServer(EntityUid target, out Entity<ChitterServerComponent> serverEnt)
    {
        serverEnt = default;

        if (TryComp<ChitterServerComponent>(target, out var direct))
        {
            serverEnt = (target, direct);
            return true;
        }

        if (!TryComp<BladeServerRackComponent>(target, out var rack))
            return false;

        foreach (var slot in rack.BladeSlots)
        {
            if (slot.Item is { } item && TryComp<ChitterServerComponent>(item, out var comp))
            {
                serverEnt = (item, comp);
                return true;
            }
        }

        return false;
    }

    private void OnDoAfter(Entity<ChitterPiCartridgeComponent> ent, ref ChitterPiScanDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target || args.Args.Used is not { } loader)
            return;

        if (!TryFindChitterServer(target, out var serverEnt))
            return;

        args.Handled = true;

        ent.Comp.ScannedServerName = Name(serverEnt);
        ent.Comp.LastScanTime = _timing.CurTime;
        ent.Comp.Chats.Clear();

        AddChats(serverEnt.Comp.Chats.Values, serverEnt.Comp, ent.Comp.Chats);
        AddChats(serverEnt.Comp.ArchivedChats.Values, serverEnt.Comp, ent.Comp.Chats);

        _audio.PlayPredicted(ScanFinishSound, loader, args.Args.User);
        _popup.PopupEntity(Loc.GetString("chitter-pi-scan-complete", ("server", ent.Comp.ScannedServerName)), loader, args.Args.User);

        UpdateUi(ent, loader);
    }

    private static void AddChats(IEnumerable<ChitterChat> source, ChitterServerComponent server, List<ChitterLogChat> destination)
    {
        foreach (var chat in source)
        {
            destination.Add(new ChitterLogChat
            {
                ChatId = chat.ChatId,
                ChatName = chat.ChatName,
                Archived = chat.Archived,
                CreatedTime = chat.CreatedTime,
                Participants = chat.ParticipantAccountIds
                    .Select(id => new ChitterLogParticipant
                    {
                        AccountId = id,
                        Name = server.Accounts.GetValueOrDefault(id)?.Name ?? $"#{id:D4}",
                    })
                    .ToList(),
                // Copy the list rather than aliasing the server's live one, so this stays a point-in-time
                // snapshot instead of silently growing as the real chat receives new messages.
                Messages = new List<ChitterMessage>(chat.Messages),
            });
        }
    }

    private void UpdateUi(Entity<ChitterPiCartridgeComponent> ent, EntityUid loader)
    {
        var state = new ChitterPiUiState
        {
            ScannedServerName = ent.Comp.ScannedServerName,
            LastScanTime = ent.Comp.LastScanTime,
            Chats = ent.Comp.Chats,
        };

        _cartridge.UpdateCartridgeUiState(loader, state);
    }
}
