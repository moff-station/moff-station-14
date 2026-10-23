using System.Collections.Immutable;
using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Timing.Systems;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Chitter;

/// This system provides APIs methods for dealing with <see cref="ChitterClientComponent"/>s of any type.
/// <seealso cref="ChitterClientSystem{T}"/>
public abstract partial class ChitterClientSystem : EntitySystem
{
    [Dependency] private ChitterCartridgeSystem _cartridge = default!;
    [Dependency] private IntrinsicChitterClientSystem _intrinsic = default!;
    [Dependency] private ChitterServerSystem _server = default!;

    [Dependency] private EntityQuery<ChitterServerComponent> _serverQuery;

    /// <inheritdoc cref="IChitterClientSystem.GetClientOrNull"/>
    [PublicAPI] public Entity<ChitterClientComponent>? GetClientOrNull(EntityUid uid) =>
        DelegateToImplementations(s => s.GetClientOrNull(uid));

    /// <inheritdoc cref="IChitterClientSystem.GetAccountOrNull"/>
    [PublicAPI] public Entity<ChitterIdentityComponent>? GetAccountOrNull(Entity<ChitterClientComponent> client) =>
        DelegateToImplementations(s => s.GetAccountOrNull(client));

    /// Gets the <see cref="ChitterServerComponent"/> that this client is connected to if the server
    /// <see cref="ChitterServerSystem.IsFunctional">is functional</see>.
    [PublicAPI] public Entity<ChitterServerComponent>? GetConnectedServer(Entity<ChitterClientComponent> client)
    {
        if (_serverQuery.ResolveOrNull(client.Comp.ConnectedServer) is { } chitterServer &&
            _server.IsFunctional(chitterServer))
            return chitterServer;

        return null;
    }

    /// <summary>
    /// Tries to connect a client to a server, or disconnect a client.
    /// </summary>
    /// <param name="client">The Chitter client entity to affect</param>
    /// <param name="connectTo">The Chitter server to connect to. If <c>null</c>, disconnects the client from all servers.</param>
    /// <param name="enforceDiscoverability">
    /// If true, a the server can only be connected to if it is <see cref="ChitterServerSystem.GetDiscoverableServers">discoverable</see>.
    /// Does nothing if <paramref name="connectTo"/> is null.
    /// </param>
    /// <returns>Whether or not the client's connected server was set to <paramref name="connectTo"/>.</returns>
    [PublicAPI] public bool Connect(
        Entity<ChitterClientComponent> client,
        Entity<ChitterServerComponent?>? connectTo,
        bool enforceDiscoverability = true
    )
    {
        if (connectTo is not { } serverNullable)
        {
            client.Comp.ConnectedServer = null;
            Dirty(client);
            return true;
        }

        Entity<ChitterServerComponent> server;
        if (enforceDiscoverability)
        {
            if (_server.GetDiscoverableServers(client).FirstOrNull(it => it.Owner == serverNullable.Owner) is not
                { } s)
                return false;
            server = s;
        }
        else
        {
            if (_serverQuery.ResolveOrNull(serverNullable) is not { } s)
                return false;
            server = s;
        }

        client.Comp.ConnectedServer = server;
        Dirty(client);
        _server.Connect(client, server);
        return true;
    }

    private T? DelegateToImplementations<T>(Func<IChitterClientSystem, T?> action) =>
        action(_intrinsic) ?? action(_cartridge);

    /// This interface defines the methods that must be implemented by any systems which
    /// <see cref="ChitterClientSystem"/> might delegate to.
    public interface IChitterClientSystem
    {
        /// Tries to get the <see cref="ChitterClientComponent"/> (and entity) most appropriate for the given entity.
        /// This may be the entity itself with the compoennt resolved, or it could be something more complex, like
        /// getting a PDA cartridge component when given a PDA entity.
        [PublicAPI] Entity<ChitterClientComponent>? GetClientOrNull(EntityUid uid);

        /// Tries to get the <see cref="ChitterIdentityComponent"/> (and entity) which the given client uses as its
        /// account information.
        [PublicAPI] Entity<ChitterIdentityComponent>? GetAccountOrNull(Entity<ChitterClientComponent> client);
    }
}

/// <summary>
/// This abstract system helps to handle UI messages from Chitter clients.
/// </summary>
/// <typeparam name="T">The data needed to interact with a chitter client.</typeparam>
/// <seealso cref="ChitterClientSystem"/>
public abstract partial class ChitterClientSystem<T> : EntitySystem, ChitterClientSystem.IChitterClientSystem
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private ChitterClientSystem _client = default!;
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private ChitterIdentitySystem _identity = default!;
    [Dependency] private ChitterServerSystem _server = default!;

    /// The key for managing <see cref="UseDelaySystem">delays</see> when interacting with chitter servers.
    private const string DelayKey = "chitter-client-server-interaction";


    // abstract redeclarations of interface methods.

    /// <inheritdoc/>
    public abstract Entity<ChitterClientComponent>? GetClientOrNull(EntityUid uid);

    /// <inheritdoc/>
    public abstract Entity<ChitterIdentityComponent>? GetAccountOrNull(Entity<ChitterClientComponent> client);


    // Private implementation helpers

    private ChitterUiState BuildUiState(T clientData)
    {
        if (GetIdentity(clientData) is not { } identity ||
            _identity.GetAccountOrNull(identity) is not { } account)
            return new ChitterUiState(new IChitterUiData.NoIdentity());

        var client = GetClient(clientData);
        var server = _client.GetConnectedServer(client);
        var contacts = new ChitterContacts(
            server?.Comp.Accounts.Values.ToImmutableArray() ?? [],
            server?.Comp.Accounts.GetValueOrDefault(account.Id, account) ?? account
        );

        return new ChitterUiState(client.Comp.OpenSubwindow switch
        {
            ChitterSubwindow.Settings => new IChitterUiData.Settings(account),
            ChitterSubwindow.ServerSelection => new IChitterUiData.ServerSelection(
                _server
                    .GetDiscoverableServers(client)
                    .Select(it => (Name(it), GetNetEntity(it)))
                    .ToImmutableArray(),
                GetNetEntity(client.Comp.ConnectedServer)
            ),
            ChitterSubwindow.NewChat => server == null
                ? new IChitterUiData.NoServer(account)
                : new IChitterUiData.NewChat(contacts),
            null => ChatViewStateData(),
            var unknown => unknown.Value.ThrowUnknownVariant<ChitterSubwindow, IChitterUiData>(),
        });

        IChitterUiData ChatViewStateData()
        {
            if (server is not { } s)
                return new IChitterUiData.NoServer(account);

            ChitterUiChatDetail? current = null;
            var chatEntries = ImmutableArray.CreateBuilder<ChitterUiChatEntry>();
            foreach (var chat in _server.GetParticipatingChats(s, account.Id))
            {
                chatEntries.Add(
                    ChitterUiChatEntry.CreateFor(
                        chat,
                        account,
                        contacts.WithSelf().AsLookupWithUnknownDefault()
                    )
                );

                if (chat.Id == client.Comp.CurrentChat)
                {
                    current = ChitterUiChatDetail.From(chat);
                }
            }

            return new IChitterUiData.Ok(
                (GetNetEntity(s), Name(s)),
                chatEntries.ToImmutable(),
                current,
                contacts
            );
        }
    }

    private (Entity<ChitterClientComponent>, Entity<ChitterIdentityComponent>, Entity<ChitterServerComponent>)?
        ResolveAccountAndServer(T clientData)
    {
        var session = GetClient(clientData);
        if (GetIdentity(clientData) is not { } account)
        {
            this.AssertOrLogError(
                $"Failed to get {nameof(ChitterIdentityComponent)} associated with {ToPrettyString(session)}");
            return null;
        }

        if (_client.GetConnectedServer(GetClient(clientData)) is not { } server)
            return null;

        return (session, account, server);
    }

    private (
        Entity<ChitterClientComponent>,
        Entity<ChitterIdentityComponent>,
        Entity<ChitterServerComponent>,
        ChitterChat
        )? ResolveAccountServerAndChat(T clientData, ChitterChatId chatId) =>
        ResolveAccountAndServer(clientData) is var (session, account, server) &&
        _server.GetParticipatingChat(server, chatId, account.Comp.AccountId) is { } chat &&
        chat.Participants.Contains(account.Comp)
            ? (session, account, server, chat)
            : null;
}
