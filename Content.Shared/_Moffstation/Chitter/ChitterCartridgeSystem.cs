using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.PDA;
using Content.Shared.Access.Systems;
using Content.Shared.CartridgeLoader;

namespace Content.Shared._Moffstation.Chitter;

/// This system makes <see cref="ChitterCartridgeComponent"/>s work.
/// It implements <see cref="ChitterClientSystem{TClient}"/> for a cartridge (PDA app) by passing around
/// both the cartridge entity and the loader (PDA) it's in.
public sealed partial class ChitterCartridgeSystem : ChitterClientSystem<
    (Entity<ChitterCartridgeComponent> Cartridge, Entity<CartridgeLoaderComponent> Loader)
>
{
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;

    [Dependency] private EntityQuery<CartridgeLoaderComponent> _cartridgeLoaderQuery;
    [Dependency] private EntityQuery<ChitterIdentityComponent> _chitterAccountQuery;
    [Dependency] private EntityQuery<ChitterCartridgeComponent> _chitterCartridgeQuery;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ChitterCartridgeComponent> entity, ref MapInitEvent args)
    {
        base.OnMapInit((entity, entity));
    }

    [SubscribeLocalEvent]
    private void OnChitterDirty(ref DirtyChitterEvent args)
    {
        foreach (var cart in EntityQueryEnumerator<ChitterCartridgeComponent>())
        {
            if (cart.Comp.ConnectedServer != args.Server ||
                _cartridgeLoader.GetLoaderOrNull(cart) is not { } loader ||
                GetIdentity((cart, loader)) is not { } account ||
                !args.Recipients.Contains(account.Comp.AccountId))
                continue;

            UpdateUi((cart, loader));
        }
    }

    [SubscribeLocalEvent]
    private void OnUiReady(Entity<ChitterCartridgeComponent> entity, ref CartridgeUiReadyEvent args) =>
        UpdateUi((entity, args.Loader));

    [SubscribeLocalEvent]
    private void OnMessage(Entity<ChitterCartridgeComponent> entity, ref CartridgeMessageEvent a)
    {
        if (a is not ChitterCartridgeMessageEvent args ||
            _cartridgeLoaderQuery.ResolveOrNull(GetEntity(args.LoaderUid)) is not { } loader)
            return;

        // We need to dispatch the inner event ourselves.
        var entAndLoader = (entity, loader);
        switch (args.Inner)
        {
            case ChitterMsg.ChooseConnectedServer inner:
                OnConnectToServer(entAndLoader, ref inner);
                break;
            case ChitterMsg.GoToMainView inner:
                OnGoToMainView(entAndLoader, ref inner);
                break;
            case ChitterMsg.CreateNewChat inner:
                OnNewChat(entAndLoader, ref inner);
                break;
            case ChitterMsg.ChangeSelectedChat inner:
                OnSelectChat(entAndLoader, ref inner);
                break;
            case ChitterMsg.SendMessage inner:
                OnSendMessage(entAndLoader, ref inner);
                break;
            case ChitterMsg.SetOwnAvatar inner:
                OnSetProfilePicture(entAndLoader, ref inner);
                break;
            case ChitterMsg.GoToNewChat inner:
                OnStartNewChat(entAndLoader, ref inner);
                break;
            case ChitterMsg.GoToSettings inner:
                OnViewSelf(entAndLoader, ref inner);
                break;
            case ChitterMsg.GoToServerSelection inner:
                OnViewServers(entAndLoader, ref inner);
                break;
            default:
                args.Inner.ThrowUnknownInheritor();
                break;
        }

        UpdateUi(entAndLoader);
    }

    /// <inheritdoc/>
    protected override Entity<ChitterClientComponent> GetClient(
        (Entity<ChitterCartridgeComponent> Cartridge, Entity<CartridgeLoaderComponent> Loader) data
    ) => (data.Cartridge, data.Cartridge);

    /// <inheritdoc/>
    protected override Entity<ChitterIdentityComponent>? GetIdentity(
        (Entity<ChitterCartridgeComponent> Cartridge, Entity<CartridgeLoaderComponent> Loader) entity
    ) => _idCard.TryGetIdCard(entity.Loader.Owner, out var id) &&
         _chitterAccountQuery.ResolveOrNull(id) is { } account
        ? account
        : null;

    /// <inheritdoc/>
    protected override void UpdateUi(
        (Entity<ChitterCartridgeComponent> Cartridge, Entity<CartridgeLoaderComponent> Loader) entity,
        ChitterUiState state
    ) => _cartridgeLoader.UpdateCartridgeUiState(entity.Loader.Owner, state, entity.Loader.Comp);

    /// <inheritdoc/>
    public override Entity<ChitterClientComponent>? GetClientOrNull(EntityUid uid)
    {
        // Try treating the given entity as a cart loader -- look for the chitter cart loaded in it.
        if (_cartridgeLoaderQuery.ResolveOrNull(uid, logMissing: false) is { } cartLoader &&
            GetLoadedChitter(cartLoader) is { } loadedCart)
            return (loadedCart, loadedCart);

        // Try treating the given entity as the chitter cart itself.
        if (_chitterCartridgeQuery.ResolveOrNull(uid, logMissing: false) is { } bareCart)
            return (bareCart, bareCart);

        return null;
    }

    private Entity<ChitterCartridgeComponent>? GetLoadedChitter(Entity<CartridgeLoaderComponent> loader) =>
        _cartridgeLoader.TryGetProgram<ChitterCartridgeComponent>(loader.AsNullable());

    /// <inheritdoc/>
    public override Entity<ChitterIdentityComponent>? GetAccountOrNull(Entity<ChitterClientComponent> client)
    {
        if (client.Comp is not ChitterCartridgeComponent cart ||
            _cartridgeLoader.GetLoaderOrNull(client) is not { } loader)
            return null;

        return GetIdentity(((client, cart), loader));
    }

    // Kinda gross that these are in here, since cartloader and ID aren't super particular to chitter, but eh.
    [SubscribeLocalEvent]
    private void OnIdInserted(Entity<CartridgeLoaderComponent> entity, ref IdInsertedIntoEvent args)
    {
        if (GetLoadedChitter(entity) is not { } client)
            return;

        UpdateUi((client, entity));
    }

    [SubscribeLocalEvent]
    private void OnIdRemoved(Entity<CartridgeLoaderComponent> entity, ref IdRemovedFromEvent args)
    {
        if (GetLoadedChitter(entity) is not { } client)
            return;

        UpdateUi((client, entity));
    }
}
