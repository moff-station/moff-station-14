using Content.Server._Moffstation.Chitter;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

public sealed partial class ChitterCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private ChitterHandlerDeps Deps => new(_server, _station, _timing, _prototypeManager, EntityManager);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeMessageEvent>(OnMessage);

        // Refresh every open Chitter window whenever anything actually changes on any server, instead
        // of polling everyone on a timer regardless of whether there's anything new to show.
        _server.DataChanged += OnServerDataChanged;
    }

    private void OnServerDataChanged()
    {
        using var query = EntityQueryEnumerator<CartridgeLoaderComponent>();
        while (query.MoveNext(out var loaderUid, out var loader))
        {
            if (loader.ActiveProgram == null)
                continue;

            if (!TryComp<ChitterCartridgeComponent>(loader.ActiveProgram.Value, out var cartridge))
                continue;

            UpdateUi((loader.ActiveProgram.Value, cartridge), loaderUid);
        }
    }

    private void OnUiReady(Entity<ChitterCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent, args.Loader);
    }

    private void OnMessage(Entity<ChitterCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not ChitterUiMessageEvent msg)
            return;

        var loader = GetEntity(args.LoaderUid);
        var discoverContacts = msg.Type is ChitterUiMessageType.RefreshContacts;

        ChitterUiMessageHandler.HandleMessage(
            ent.Comp,
            msg,
            (bool requirePower, out Entity<ChitterServerComponent> server, out Entity<ChitterAccountComponent> account, out string name, out string jobTitle)
                => TryGetContext(loader, requirePower, out server, out account, out name, out jobTitle),
            Deps);

        UpdateUi(ent, loader, discoverContacts);
    }

    // Resolves "who is asking" for a PDA: the server reachable from the loader (grid-based, or its
    // manually connected M.P.N. one), and the account living on whatever ID card is inserted - CentCom
    // IDs and cardless PDAs don't get a Chitter identity at all.
    private bool TryGetContext(
        EntityUid loader,
        bool requirePower,
        out Entity<ChitterServerComponent> server,
        out Entity<ChitterAccountComponent> account,
        out string name,
        out string jobTitle)
    {
        server = default;
        account = default;
        name = "Unknown";
        jobTitle = "Unknown";

        var found = requirePower
            ? _server.TryFindServer(loader, out server)
            : _server.TryFindServerAnyPower(loader, out server);

        if (!found)
            return false;

        if (!_server.TryGetPdaIdCard(loader, out var idCard))
            return false;

        if (HasCentComAccess(idCard))
            return false;

        if (!TryComp<ChitterAccountComponent>(idCard, out var foundAccount))
            return false;

        account = (idCard, foundAccount);

        if (TryComp<IdCardComponent>(idCard, out var idCardComp))
        {
            name = idCardComp.FullName ?? "Unknown";
            jobTitle = idCardComp.LocalizedJobTitle ?? "Unknown";
        }

        return true;
    }

    private bool HasCentComAccess(EntityUid uid)
    {
        return TryComp<AccessComponent>(uid, out var access) && access.Tags.Contains("CentralCommand");
    }

    // Called externally (by ChitterMpnSystem right after connecting/disconnecting a private network) to
    // force an immediate refresh - otherwise an already-open Chitter window wouldn't pick up the change
    // until the next periodic Update() poll, or until the player closed and reopened the app.
    public void RefreshUi(Entity<ChitterCartridgeComponent> ent, EntityUid loader)
    {
        UpdateUi(ent, loader);
    }

    private void UpdateUi(Entity<ChitterCartridgeComponent> ent, EntityUid loader, bool discoverContacts = true)
    {
        var hasIdCard = _server.TryGetPdaIdCard(loader, out var idCard);
        var serverOnline = _server.TryFindServer(loader, out var serverEnt);

        var state = new ChitterUiState
        {
            HasIdCard = hasIdCard,
            ServerOnline = serverOnline,
        };

        if (hasIdCard && HasCentComAccess(idCard))
        {
            hasIdCard = false;
            state.HasIdCard = false;
        }

        if (hasIdCard && TryComp<ChitterAccountComponent>(idCard, out var account))
        {
            state.OwnNumber = account.AccountId;
            state.OwnProfilePicture = account.ProfilePictureId;

            var ownerName = "Unknown";
            var ownerJobTitle = "Unknown";
            if (TryComp<IdCardComponent>(idCard, out var idCardComp))
            {
                ownerName = idCardComp.FullName ?? "Unknown";
                ownerJobTitle = idCardComp.LocalizedJobTitle ?? "Unknown";
            }

            state.OwnName = ownerName;
            state.OwnJob = ownerJobTitle;

            if (serverOnline)
            {
                ChitterUiMessageHandler.PopulateState(
                    state,
                    serverEnt,
                    account.AccountId,
                    ownerName,
                    ownerJobTitle,
                    account.ProfilePictureId,
                    ent.Comp.CurrentChatId,
                    discoverContacts,
                    loader,
                    Deps);
            }
        }

        _cartridge.UpdateCartridgeUiState(loader, state);
    }
}
