using Content.Server.Power.Components;
using Content.Server._Moffstation.Power.Components;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.PDA;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Chitter;

public sealed class ChitterServerSystem : SharedChitterSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IReplayRecordingManager _replay = default!;

    private const int MessageCharLimit = 500;
    private const int ChatNameCharLimit = 50;
    private const int MaxChatParticipants = 20;

    // Fired whenever a chat/message/participant mutates, so the admin log panel can push a fresh
    // state to anyone with it open instead of only showing a snapshot from when it was opened.
    public event Action? DataChanged;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool TryFindServer(EntityUid loader, out Entity<ChitterServerComponent> server)
    {
        return TryFindServer(loader, requirePowered: true, out server);
    }

    // Ignores power so a caller can still resolve the server to e.g. record a failed delivery.
    public bool TryFindServerAnyPower(EntityUid loader, out Entity<ChitterServerComponent> server)
    {
        return TryFindServer(loader, requirePowered: false, out server);
    }

    private bool TryFindServer(EntityUid loader, bool requirePowered, out Entity<ChitterServerComponent> server)
    {
        server = default;

        var loaderGrid = Transform(loader).GridUid;
        if (!loaderGrid.HasValue)
            return false;

        // Picks the lowest-UID match instead of just the first one the enumerator happens to hit,
        // so repeated calls resolve to the same server when more than one is in range - otherwise
        // a handler and the UpdateUi call right after it could silently disagree on which server a
        // chat was even created on.
        var found = false;

        using (var query = EntityQueryEnumerator<ChitterServerComponent>())
        while (query.MoveNext(out var uid, out var comp))
        {
            if (requirePowered && !IsServerPowered((uid, comp)))
                continue;

            var serverGrid = Transform(uid).GridUid;

            var matches = serverGrid == loaderGrid ||
                (serverGrid == null && TryComp<InnerCableReceiverComponent>(uid, out var receiver)
                    && receiver.Provider is {} provider
                    && Transform(provider.Owner).GridUid == loaderGrid);

            if (!matches)
                continue;

            if (!found || uid.Id < server.Owner.Id)
            {
                server = (uid, comp);
                found = true;
            }
        }

        return found;
    }

    public bool IsServerPowered(Entity<ChitterServerComponent> server)
    {
        if (TryComp<ApcPowerReceiverComponent>(server, out var apcPower) && apcPower.Powered)
            return true;

        if (TryComp<InnerCableReceiverComponent>(server, out var receiver) && receiver.Provider is {} provider)
            return TryComp<ApcPowerReceiverComponent>(provider, out var rackPower) && rackPower.Powered;

        return false;
    }

    public bool TryGetPdaIdCard(EntityUid loader, out EntityUid idCard)
    {
        idCard = default;

        if (!TryComp<PdaComponent>(loader, out var pda) || pda.ContainedId == null)
            return false;

        idCard = pda.ContainedId.Value;
        return true;
    }

    public ChitterAccount? GetAccount(ChitterServerComponent server, uint accountId)
    {
        return server.Accounts.GetValueOrDefault(accountId);
    }

    public void RegisterOrUpdateAccount(ChitterServerComponent server, uint accountId, string name, string jobTitle, string profilePictureId)
    {
        server.Accounts[accountId] = new ChitterAccount
        {
            AccountId = accountId,
            Name = name,
            JobTitle = jobTitle,
            ProfilePictureId = profilePictureId,
        };
    }

    public ChitterChat? GetChat(ChitterServerComponent server, Guid chatId)
    {
        if (server.Chats.TryGetValue(chatId, out var chat))
            return chat;

        return server.ArchivedChats.GetValueOrDefault(chatId);
    }

    public Guid CreateChat(ChitterServerComponent server, List<uint> participants, string? chatName = null)
    {
        if (participants.Count > MaxChatParticipants)
            participants = participants.GetRange(0, MaxChatParticipants);

        var chat = new ChitterChat
        {
            ChatId = Guid.NewGuid(),
            ChatName = TruncateChatName(chatName),
            ParticipantAccountIds = participants,
            CreatedTime = _timing.CurTime,
        };
        server.Chats[chat.ChatId] = chat;
        DataChanged?.Invoke();
        return chat.ChatId;
    }

    public void RenameChat(ChitterServerComponent server, Guid chatId, string? chatName)
    {
        if (!server.Chats.TryGetValue(chatId, out var chat))
            return;

        chat.ChatName = TruncateChatName(chatName);
        DataChanged?.Invoke();
    }

    private static string TruncateChatName(string? chatName)
    {
        if (chatName == null)
            return string.Empty;

        return chatName.Length > ChatNameCharLimit
            ? chatName[..ChatNameCharLimit]
            : chatName;
    }

    public bool AddMessage(ChitterServerComponent server, Guid chatId, uint senderId, string senderName, string content)
    {
        if (!server.Chats.TryGetValue(chatId, out var chat))
            return false;

        if (content.Length > MessageCharLimit)
            content = content[..MessageCharLimit];

        var message = new ChitterMessage
        {
            MessageId = Guid.NewGuid(),
            SenderAccountId = senderId,
            SenderName = senderName,
            Timestamp = _timing.CurTime,
            Content = content,
        };

        chat.Messages.Add(message);

        // Same pattern as RadioSystem/ChatManager - lets messages be pulled from a saved replay later.
        _replay.RecordServerMessage(new ChitterReplayMessageRecord { ChatId = chatId, Message = message });

        DataChanged?.Invoke();
        return true;
    }

    public void ArchiveChat(ChitterServerComponent server, Guid chatId)
    {
        if (!server.Chats.Remove(chatId, out var chat))
            return;

        chat.Archived = true;
        server.ArchivedChats[chatId] = chat;
        DataChanged?.Invoke();
    }

    public void AddParticipantToChat(ChitterServerComponent server, Guid chatId, uint accountId)
    {
        if (server.Chats.TryGetValue(chatId, out var chat) && !chat.ParticipantAccountIds.Contains(accountId))
        {
            chat.ParticipantAccountIds.Add(accountId);
            DataChanged?.Invoke();
        }
    }

    public void RemoveParticipantFromChat(ChitterServerComponent server, Guid chatId, uint accountId)
    {
        if (server.Chats.TryGetValue(chatId, out var chat) && chat.ParticipantAccountIds.Remove(accountId))
            DataChanged?.Invoke();
    }

    public void MarkDeliveryFailed(ChitterServerComponent server, Guid chatId)
    {
        if (!server.Chats.TryGetValue(chatId, out var chat) || chat.Messages.Count == 0)
            return;

        chat.Messages[^1].DeliveryFailed = true;
    }

    // Used by the admin log panel to list conversations from every station, not just one server.
    public IEnumerable<ChitterServerComponent> GetAllServers()
    {
        var query = EntityQueryEnumerator<ChitterServerComponent>();
        while (query.MoveNext(out var server))
        {
            yield return server;
        }
    }
}
