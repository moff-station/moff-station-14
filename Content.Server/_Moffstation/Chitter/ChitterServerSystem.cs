using System.Linq;
using Content.Server.Power.Components;
using Content.Server._Moffstation.Power.Components;
using Content.Shared._Moffstation.BladeServer;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.PDA;
using Content.Shared.Power;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Chitter;

public sealed partial class ChitterServerSystem : SharedChitterSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IReplayRecordingManager _replay = default!;

    private const int MessageCharLimit = 500;
    private const int ChatNameCharLimit = 50;
    private const int MaxChatParticipants = 20;

    // Fired whenever anything that could change how a Chitter server looks to a viewer happens - a
    // chat/message/participant mutates, a server gains/loses power, or a server is destroyed - so
    // anyone with a Chitter UI open (the admin log panel, PDAs, the AI) can push a fresh state instead
    // of only showing a snapshot from whenever they last looked.
    public event Action? DataChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChitterServerComponent, PowerChangedEvent>(OnServerPowerChanged);
        SubscribeLocalEvent<ChitterServerComponent, ComponentShutdown>(OnServerShutdown);

        // Racked blade servers (the common case) don't have their own ApcPowerReceiver - they draw
        // through the rack's, so the rack is what actually receives power changes. M.P.N. servers
        // report power a third way (PowerConsumerReceivedChanged) and notify via NotifyDataChanged
        // from ChitterMpnSystem instead, since that event isn't power-receiver-based at all.
        SubscribeLocalEvent<BladeServerRackComponent, BladeServerRackPowerChangedEvent>(OnRackPowerChanged);
    }

    private void OnServerPowerChanged(Entity<ChitterServerComponent> ent, ref PowerChangedEvent args)
    {
        DataChanged?.Invoke();
    }

    private void OnServerShutdown(Entity<ChitterServerComponent> ent, ref ComponentShutdown args)
    {
        DataChanged?.Invoke();
    }

    private void OnRackPowerChanged(Entity<BladeServerRackComponent> ent, ref BladeServerRackPowerChangedEvent args)
    {
        foreach (var slot in ent.Comp.BladeSlots)
        {
            if (slot.Item is { } item && HasComp<ChitterServerComponent>(item))
            {
                DataChanged?.Invoke();
                return;
            }
        }
    }

    // For callers that can't invoke DataChanged directly (it's only invocable from within this class) -
    // currently just ChitterMpnSystem, whose M.P.N. servers report power via PowerConsumerReceivedChanged
    // rather than the ApcPowerReceiver-based PowerChangedEvent this system otherwise listens for.
    public void NotifyDataChanged()
    {
        DataChanged?.Invoke();
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

        // A PDA that's manually connected to a private M.P.N. server stays on it exclusively - it never
        // silently falls back to the station's own server, even if the M.P.N. one is out of power.
        if (TryComp<ChitterMpnConnectionComponent>(loader, out var mpnConnection) && mpnConnection.ConnectedServer is { } connected)
        {
            if (TerminatingOrDeleted(connected) || !TryComp<ChitterServerComponent>(connected, out var connectedComp))
            {
                mpnConnection.ConnectedServer = null;
                Dirty(loader, mpnConnection);
                return false;
            }

            if (requirePowered && !IsServerPowered((connected, connectedComp)))
                return false;

            server = (connected, connectedComp);
            return true;
        }

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
            // Private M.P.N. servers never show up in the normal grid-wide scan - they're only reachable
            // by manually connecting to them (see above).
            if (HasComp<ChitterMpnServerComponent>(uid))
                continue;

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

        // M.P.N. servers skip the APC/extension-cable network entirely and draw straight off an MV cable.
        if (TryComp<PowerConsumerComponent>(server, out var consumer))
            return consumer.ReceivedPower >= consumer.DrawRate;

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

    // Used when an account swaps over to a private M.P.N. server (or back) - it shouldn't keep showing
    // up as a contact on a network it's no longer actually reachable through.
    public void RemoveAccount(ChitterServerComponent server, uint accountId)
    {
        if (server.Accounts.Remove(accountId))
            DataChanged?.Invoke();
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
        DataChanged?.Invoke();
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

    // Shared by the admin log panel and the Chitter P.I. cartridge, which both need to turn a server's
    // live ChitterChats into read-only log DTOs. Copies each chat's message list rather than aliasing
    // the server's live one, so a caller holding onto the result (like P.I.'s snapshot) doesn't
    // silently keep growing as the real conversation continues.
    public static List<ChitterLogChat> BuildLogChats(IEnumerable<ChitterChat> source, ChitterServerComponent server)
    {
        var result = new List<ChitterLogChat>();

        foreach (var chat in source)
        {
            result.Add(new ChitterLogChat
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
                Messages = new List<ChitterMessage>(chat.Messages),
            });
        }

        return result;
    }
}
