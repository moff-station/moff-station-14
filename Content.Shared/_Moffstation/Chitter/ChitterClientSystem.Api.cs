using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Chitter;

// This part contains all of the public and protected methods on this class.
public abstract partial class ChitterClientSystem<T>
{
    // Abstract methods for implementing the rest of the protected members.

    /// Gets the entity with <see cref="ChitterClientComponent"/> from <typeparamref name="T"/>.
    protected abstract Entity<ChitterClientComponent> GetClient(T clientData);

    /// Gets the entity with <see cref="ChitterIdentityComponent"/> from <typeparamref name="T"/> which drives this
    /// client. Returns nullable because we may not have an account, eg. if we're a PDA without an ID card.
    protected abstract Entity<ChitterIdentityComponent>? GetIdentity(T clientData);

    /// Applies the given <paramref name="state"/> to the given client.
    protected abstract void UpdateUi(T clientData, ChitterUiState state);


    // Protected members for use in implementing client behavior.

    /// Causes the given client to update its UI.
    protected void UpdateUi(T clientData) => UpdateUi(clientData, BuildUiState(clientData));

    protected void OnMapInit<TComp>(Entity<TComp> entity) where TComp : ChitterClientComponent
    {
        _delay.SetLength(entity.Owner, entity.Comp.ServerInteractionDelay, id: DelayKey);
    }

    protected void OnNewChat(T clientData, ref ChitterMsg.CreateNewChat args)
    {
        if (_timing is { ApplyingState: false, IsFirstTimePredicted: false } ||
            ResolveAccountAndServer(clientData) is not var (client, account, server))
            return;

        HashSet<ChitterAccountId> participants = [account.Comp.AccountId, .. args.AccountIds];
        if (participants.Count < 2 ||
            !_delay.TryResetDelay(client.Owner, id: DelayKey))
            return;

        client.Comp.CurrentChat = _server.CreateChat(server, participants, args.ChatName);
        client.Comp.OpenSubwindow = null;
        Dirty(client);
        UpdateUi(clientData);
    }

    protected void OnSelectChat(T clientData, ref ChitterMsg.ChangeSelectedChat args)
    {
        var client = GetClient(clientData);
        client.Comp.CurrentChat = args.ChatId;
        Dirty(client);
        UpdateUi(clientData);
    }

    protected void OnSendMessage(T clientData, ref ChitterMsg.SendMessage args)
    {
        if (args.Content == "" ||
            _timing is { ApplyingState: false, IsFirstTimePredicted: false } ||
            ResolveAccountServerAndChat(clientData, args.ChatId) is not var (client, account, server, chat) ||
            !_delay.TryResetDelay(client.Owner, id: DelayKey))
            return;

        _server.AddMessage(server, chat.Id, account.Comp.AccountId, args.Content);
        UpdateUi(clientData);
    }

    protected void OnSetProfilePicture(T clientData, ref ChitterMsg.SetOwnAvatar args)
    {
        if (GetIdentity(clientData) is not { } account)
            return;
        _identity.SetAvatar(account, args.Proto);

        if (_client.GetConnectedServer(GetClient(clientData)) is not { } server)
            return;
        _server.SetProfilePicture(server, account.Comp, args.Proto);
        UpdateUi(clientData);
    }

    protected void OnConnectToServer(T clientData, ref ChitterMsg.ChooseConnectedServer args)
    {
        var client = GetClient(clientData);

        if (_client.Connect(client, GetEntity(args.Server)))
        {
            client.Comp.OpenSubwindow = null;
            Dirty(client);
        }
        UpdateUi(clientData);
    }

    protected void OnGoToMainView(T clientData, ref ChitterMsg.GoToMainView args)
    {
        var client = GetClient(clientData);
        client.Comp.OpenSubwindow = null;
        Dirty(client);
        UpdateUi(clientData);
    }

    protected void OnViewSelf(T clientData, ref ChitterMsg.GoToSettings args)
    {
        var client = GetClient(clientData);
        client.Comp.OpenSubwindow = ChitterSubwindow.Settings;
        Dirty(client);
        UpdateUi(clientData);
    }

    protected void OnViewServers(T clientData, ref ChitterMsg.GoToServerSelection args)
    {
        var client = GetClient(clientData);
        client.Comp.OpenSubwindow = ChitterSubwindow.ServerSelection;
        Dirty(client);
        UpdateUi(clientData);
    }

    protected void OnStartNewChat(T clientData, ref ChitterMsg.GoToNewChat args)
    {
        var client = GetClient(clientData);
        client.Comp.OpenSubwindow = ChitterSubwindow.NewChat;
        Dirty(client);
        UpdateUi(clientData);
    }
}
