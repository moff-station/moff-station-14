using Content.Server._Moffstation.CartridgeLoader.Cartridges;
using Content.Server.Power.EntitySystems;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.CartridgeLoader;
using Content.Shared.Interaction;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Power;

namespace Content.Server._Moffstation.Chitter;

// Handles connecting a PDA's Chitter app to a private M.P.N. server (left-clicking a powered one with the
// Chitter cartridge installed - it doesn't need to be the open program) and disconnecting from it again
// (the "Leave Network" button on the PDA's own status banner, sent as a normal PDA UI message rather than
// through the cartridge).
public sealed partial class ChitterMpnSystem : EntitySystem
{
    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private ChitterCartridgeSystem _chitterCartridge = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeRelayedEvent<AfterInteractEvent>>(OnAfterInteract);
        SubscribeLocalEvent<ChitterMpnServerComponent, PowerConsumerReceivedChanged>(OnPowerChanged);
        SubscribeLocalEvent<PdaComponent, PdaDisconnectMpnMessage>(OnDisconnectMessage);
    }

    // PowerConsumerComponent (unlike ApcPowerReceiverComponent) doesn't drive appearance data on its own,
    // so the sprite's Powered visual needs to be pushed manually here. It also doesn't raise the
    // ApcPowerReceiver-based PowerChangedEvent ChitterServerSystem otherwise listens for, so anyone
    // with a Chitter UI open needs to be told about the power flip separately too.
    private void OnPowerChanged(Entity<ChitterMpnServerComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, args.ReceivedPower >= args.DrawRate);
        _server.NotifyDataChanged();
    }

    private void OnAfterInteract(Entity<ChitterCartridgeComponent> ent, ref CartridgeRelayedEvent<AfterInteractEvent> args)
    {
        if (args.Args.Handled || !args.Args.CanReach || args.Args.Target is not { } target)
            return;

        if (!HasComp<ChitterMpnServerComponent>(target) || !TryComp<ChitterServerComponent>(target, out var serverComp))
            return;

        args.Args.Handled = true;

        var loader = args.Loader.Owner;
        var user = args.Args.User;

        if (!_server.IsServerPowered((target, serverComp)))
        {
            _popup.PopupEntity(Loc.GetString("chitter-mpn-not-powered"), loader, user);
            return;
        }

        // Leaves whatever server this account was previously reachable on (usually the station's) -
        // it shouldn't keep showing up as a contact there while connected to a private network instead.
        var previous = RemoveFromCurrentServer(loader);

        var connection = EnsureComp<ChitterMpnConnectionComponent>(loader);
        connection.ConnectedServer = target;
        connection.ServerName = Name(target);
        Dirty(loader, connection);

        RefreshChitterCartridge(loader);
        RefreshUsersOf(target, exclude: loader);
        if (previous is { } prev && prev != target)
            RefreshUsersOf(prev, exclude: loader);

        _popup.PopupEntity(Loc.GetString("chitter-mpn-connected", ("server", connection.ServerName)), loader, user);
    }

    private void OnDisconnectMessage(EntityUid uid, PdaComponent pda, PdaDisconnectMpnMessage msg)
    {
        if (!TryComp<ChitterMpnConnectionComponent>(uid, out var connection) || connection.ConnectedServer is not { } server)
            return;

        // Leaving the M.P.N. server the same way - drop off its contact list before falling back to
        // whatever the normal grid-based lookup resolves to (usually the station's server again).
        RemoveFromCurrentServer(uid);
        connection.ConnectedServer = null;
        Dirty(uid, connection);

        RefreshChitterCartridge(uid);
        RefreshUsersOf(server, exclude: uid);
        if (_server.TryFindServerAnyPower(uid, out var rejoined) && rejoined.Owner != server)
            RefreshUsersOf(rejoined.Owner, exclude: uid);

        _popup.PopupEntity(Loc.GetString("chitter-mpn-disconnected"), uid, msg.Actor);
    }

    // Removes this loader's account from whatever server it's currently reachable on (if any), and
    // returns that server so the caller can refresh everyone else still on it.
    private EntityUid? RemoveFromCurrentServer(EntityUid loader)
    {
        if (!_server.TryFindServerAnyPower(loader, out var current))
            return null;

        if (_server.TryGetPdaIdCard(loader, out var idCard) && TryComp<ChitterAccountComponent>(idCard, out var account))
            _server.RemoveAccount(current.Comp, account.AccountId);

        return current.Owner;
    }

    // Refreshes every PDA whose Chitter currently resolves to the given server (station or M.P.N.),
    // excluding the acting loader, so their contact list reflects an account joining or leaving
    // immediately instead of waiting for the periodic 3-second poll.
    private void RefreshUsersOf(EntityUid server, EntityUid exclude)
    {
        var query = EntityQueryEnumerator<CartridgeLoaderComponent>();
        while (query.MoveNext(out var loader, out _))
        {
            if (loader == exclude)
                continue;

            if (_server.TryFindServerAnyPower(loader, out var resolved) && resolved.Owner == server)
                RefreshChitterCartridge(loader);
        }
    }

    // Only refreshes if Chitter is actually the loader's current foreground program - pushing its state
    // otherwise would land on whatever cartridge Control the client currently has attached instead.
    private void RefreshChitterCartridge(EntityUid loader)
    {
        if (!TryComp<CartridgeLoaderComponent>(loader, out var cartridgeLoader) || cartridgeLoader.ActiveProgram is not { } active)
            return;

        if (TryComp<ChitterCartridgeComponent>(active, out var cartridge))
            _chitterCartridge.RefreshUi((active, cartridge), loader);
    }
}
