using Content.Shared._Moffstation.Extensions;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

/// This system makes <see cref="IntrinsicChitterClientComponent"/>s work.
/// It implements <see cref="ChitterClientSystem{TClient}"/> for a BUI.
public sealed partial class IntrinsicChitterClientSystem : ChitterClientSystem<Entity<IntrinsicChitterClientComponent>>
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [Dependency] private EntityQuery<ChitterIdentityComponent> _chitterAccountQuery;
    [Dependency] private EntityQuery<IntrinsicChitterClientComponent> _intrinsicQuery;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<IntrinsicChitterClientComponent>(
            IntrinsicChitterUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>((entity, ref _) => UpdateUi(entity));
                subs.Event<ChitterMsg.CreateNewChat>(OnNewChat);
                subs.Event<ChitterMsg.ChangeSelectedChat>(OnSelectChat);
                subs.Event<ChitterMsg.SendMessage>(OnSendMessage);
                subs.Event<ChitterMsg.SetOwnAvatar>(OnSetProfilePicture);
                subs.Event<ChitterMsg.GoToMainView>(OnGoToMainView);
                subs.Event<ChitterMsg.ChooseConnectedServer>(OnConnectToServer);
                subs.Event<ChitterMsg.GoToNewChat>(OnStartNewChat);
                subs.Event<ChitterMsg.GoToServerSelection>(OnViewServers);
                subs.Event<ChitterMsg.GoToSettings>(OnViewSelf);
            }
        );
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<IntrinsicChitterClientComponent> entity, ref MapInitEvent args)
    {
        base.OnMapInit((entity, entity));
    }

    [SubscribeLocalEvent]
    private void OnChitterDirty(ref DirtyChitterEvent args)
    {
        foreach (var cart in EntityQueryEnumerator<IntrinsicChitterClientComponent>())
        {
            if (cart.Comp.ConnectedServer != args.Server ||
                GetIdentity(cart) is not { } account ||
                !args.Recipients.Contains(account.Comp.AccountId))
                continue;

            UpdateUi(cart);
        }
    }

    /// <inheritdoc/>
    protected override Entity<ChitterClientComponent> GetClient(Entity<IntrinsicChitterClientComponent> data) =>
        (data, data);

    /// <inheritdoc/>
    protected override Entity<ChitterIdentityComponent>? GetIdentity(
        Entity<IntrinsicChitterClientComponent> entity
    ) => _chitterAccountQuery.ResolveOrNull(entity);

    /// <inheritdoc/>
    protected override void UpdateUi(Entity<IntrinsicChitterClientComponent> entity, ChitterUiState state) =>
        _ui.SetUiState(entity.Owner, IntrinsicChitterUiKey.Key, state);

    public override Entity<ChitterClientComponent>? GetClientOrNull(EntityUid uid) =>
        _intrinsicQuery.ResolveOrNull(uid, logMissing: false) is { } c ? (c, c) : null;

    public override Entity<ChitterIdentityComponent>? GetAccountOrNull(Entity<ChitterClientComponent> client) =>
        client.Comp is IntrinsicChitterClientComponent intrinsic ? GetIdentity((client, intrinsic)) : null;
}

[Serializable, NetSerializable] public enum IntrinsicChitterUiKey : byte { Key }
