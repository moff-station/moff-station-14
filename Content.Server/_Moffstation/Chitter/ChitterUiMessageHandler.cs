using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Chitter;

// Resolves "who is asking" for a Chitter action - true if a server was found (and, if requirePower,
// that it's powered), plus the account, display name, and job title to act as. ChitterCartridgeSystem
// and ChitterAiSystem each implement this their own way (ID card lookup vs. a bare AI entity).
public delegate bool ChitterContextResolver(
    bool requirePower,
    out Entity<ChitterServerComponent> server,
    out Entity<ChitterAccountComponent> account,
    out string name,
    out string jobTitle);

// Bundles the systems ChitterUiMessageHandler needs so callers don't have to pass five separate
// dependencies through every method.
public readonly record struct ChitterHandlerDeps(
    ChitterServerSystem Server,
    StationSystem Station,
    IGameTiming Timing,
    IPrototypeManager Prototypes,
    IEntityManager EntityManager);

// Shared message-handling and UI-state-building logic for anything that resolves to a Chitter account
// and exposes a ChitterUiState. The PDA cartridge (ChitterCartridgeSystem) and the station AI's
// intrinsic panel (ChitterAiSystem) are the same feature once "who is asking" has been resolved - this
// is that shared middle layer, so a fix or a rule change (like excluding private M.P.N. servers from
// contact auto-discovery) only has to happen in one place.
public static class ChitterUiMessageHandler
{
    private static readonly TimeSpan MessageCooldown = TimeSpan.FromSeconds(1);

    public static void HandleMessage(IChitterSession session, IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        switch (msg.Type)
        {
            case ChitterUiMessageType.NewChat:
                HandleNewChat(session, msg, resolve, deps);
                break;
            case ChitterUiMessageType.SelectChat:
                HandleSelectChat(session, msg);
                break;
            case ChitterUiMessageType.SendMessage:
                HandleSendMessage(session, msg, resolve, deps);
                break;
            case ChitterUiMessageType.LeaveChat:
                HandleLeaveChat(msg, resolve, deps);
                break;
            case ChitterUiMessageType.AddParticipant:
                HandleAddParticipant(msg, resolve, deps);
                break;
            case ChitterUiMessageType.RemoveParticipant:
                HandleRemoveParticipant(msg, resolve, deps);
                break;
            case ChitterUiMessageType.ArchiveChat:
                HandleArchiveChat(msg, resolve, deps);
                break;
            case ChitterUiMessageType.SetProfilePicture:
                HandleSetProfilePicture(msg, resolve, deps);
                break;
            case ChitterUiMessageType.RefreshContacts:
                // Triggers an immediate UI refresh via the caller's UpdateUi right after this returns.
                break;
            case ChitterUiMessageType.RenameChat:
                HandleRenameChat(msg, resolve, deps);
                break;
        }
    }

    // Only returns the chat if accountId is actually a participant, so every mutating handler can
    // guard against acting on a chat the caller isn't part of. Archived chats don't count either -
    // they shouldn't accept renames, new messages, or participant changes.
    private static bool TryGetParticipantChat(ChitterServerComponent server, Guid chatId, uint accountId, out ChitterChat chat)
    {
        chat = default!;

        if (!server.Chats.TryGetValue(chatId, out var foundChat))
            return false;

        if (!foundChat.ParticipantAccountIds.Contains(accountId))
            return false;

        chat = foundChat;
        return true;
    }

    private static void HandleNewChat(IChitterSession session, IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        if (!resolve(true, out var serverEnt, out var account, out _, out _))
            return;

        if (deps.Timing.CurTime < session.NextChatAllowed)
            return;

        var ownId = account.Comp.AccountId;

        List<uint> participants;
        if (msg.TargetNumbers != null && msg.TargetNumbers.Count > 0)
        {
            participants = new List<uint> { ownId };
            foreach (var target in msg.TargetNumbers)
            {
                if (target != ownId && !participants.Contains(target))
                    participants.Add(target);
            }
        }
        else if (msg.TargetNumber != null && msg.TargetNumber != ownId)
        {
            participants = new List<uint> { ownId, msg.TargetNumber.Value };
        }
        else
        {
            return;
        }

        session.NextChatAllowed = deps.Timing.CurTime + MessageCooldown;

        var chatId = deps.Server.CreateChat(serverEnt.Comp, participants, msg.ChatName);
        session.CurrentChatId = chatId;
    }

    private static void HandleSelectChat(IChitterSession session, IChitterUiMessage msg)
    {
        if (msg.ChatId == null)
            return;

        session.CurrentChatId = msg.ChatId;
    }

    private static void HandleSendMessage(IChitterSession session, IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        // Resolve the server regardless of power so an already-unpowered server still gets its
        // message recorded and marked as failed, instead of the send silently doing nothing.
        if (!resolve(false, out var serverEnt, out var account, out var name, out _))
            return;

        if (msg.ChatId == null || string.IsNullOrWhiteSpace(msg.Content))
            return;

        if (deps.Timing.CurTime < session.NextMessageAllowed)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        session.NextMessageAllowed = deps.Timing.CurTime + MessageCooldown;

        deps.Server.AddMessage(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, name, msg.Content);

        if (!deps.Server.IsServerPowered(serverEnt))
            deps.Server.MarkDeliveryFailed(serverEnt.Comp, msg.ChatId.Value);
    }

    private static void HandleLeaveChat(IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        if (!resolve(true, out var serverEnt, out var account, out _, out _))
            return;

        if (msg.ChatId != null)
            deps.Server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId);
    }

    private static void HandleAddParticipant(IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        if (!resolve(true, out var serverEnt, out var account, out _, out _))
            return;

        if (msg.ChatId == null || msg.TargetNumber == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        deps.Server.AddParticipantToChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private static void HandleRemoveParticipant(IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        if (!resolve(true, out var serverEnt, out var account, out _, out _))
            return;

        if (msg.ChatId == null || msg.TargetNumber == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        deps.Server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private static void HandleArchiveChat(IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        if (!resolve(true, out var serverEnt, out var account, out _, out _))
            return;

        if (msg.ChatId == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        deps.Server.ArchiveChat(serverEnt.Comp, msg.ChatId.Value);
    }

    private static void HandleRenameChat(IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        if (!resolve(true, out var serverEnt, out var account, out _, out _))
            return;

        if (msg.ChatId == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        deps.Server.RenameChat(serverEnt.Comp, msg.ChatId.Value, msg.ChatName);
    }

    private static void HandleSetProfilePicture(IChitterUiMessage msg, ChitterContextResolver resolve, ChitterHandlerDeps deps)
    {
        if (!resolve(true, out var serverEnt, out var account, out var name, out var jobTitle))
            return;

        // Once a server's identity records have been scrambled, nobody can pick a new picture on it -
        // it would just get overwritten by the disguise on the next refresh anyway.
        if (serverEnt.Comp.Emagged)
            return;

        if (msg.ProfilePictureId == null
            || !deps.Prototypes.TryIndex<ChitterAvatarPrototype>(msg.ProfilePictureId, out var avatar)
            || avatar.Hidden)
            return;

        account.Comp.ProfilePictureId = msg.ProfilePictureId;
        deps.EntityManager.Dirty(account.Owner, account.Comp);

        deps.Server.RegisterOrUpdateAccount(serverEnt.Comp, account.Comp.AccountId, name, jobTitle, msg.ProfilePictureId);
    }

    // Populates contacts/chats/current-chat onto an already-initialized ChitterUiState, given a
    // server + account the caller has already resolved its own way. Deliberately doesn't touch
    // state.HasIdCard/ServerOnline or the "own name/job" fields - those need to keep working even when
    // no server is reachable at all (e.g. still showing your own Chitter number), which differs enough
    // between callers that each one builds that part itself before calling this.
    public static void PopulateState(
        ChitterUiState state,
        Entity<ChitterServerComponent> server,
        uint accountId,
        string name,
        string jobTitle,
        string profilePictureId,
        Guid? currentChatId,
        bool discoverContacts,
        EntityUid discoveryContext,
        ChitterHandlerDeps deps)
    {
        var serverComp = server.Comp;
        deps.Server.RegisterOrUpdateAccount(serverComp, accountId, name, jobTitle, profilePictureId);

        // The grid-wide scan below is comparatively expensive; only run it on the periodic refresh (or
        // an explicit RefreshContacts request), not after every single message. Private M.P.N. servers
        // are excluded entirely - auto-adding every ID card on the station would defeat the point of a
        // private network.
        if (discoverContacts && !deps.EntityManager.HasComponent<ChitterMpnServerComponent>(server.Owner))
            DiscoverAccountsOnGrid(discoveryContext, serverComp, deps);

        foreach (var (accId, acc) in serverComp.Accounts)
        {
            if (accId == accountId)
                continue;
            state.Contacts.Add(new AccountEntry
            {
                AccountId = accId,
                Name = acc.Name,
                JobTitle = acc.JobTitle,
                ProfilePictureId = acc.ProfilePictureId,
            });
        }

        foreach (var (chatId, chat) in serverComp.Chats)
        {
            if (!chat.ParticipantAccountIds.Contains(accountId))
                continue;

            var lastMsg = chat.Messages.Count > 0 ? chat.Messages[^1].Content : "";
            var displayName = !string.IsNullOrWhiteSpace(chat.ChatName)
                ? chat.ChatName
                : string.Join(", ",
                    chat.ParticipantAccountIds
                        .Where(id => id != accountId)
                        .Select(id => serverComp.Accounts.GetValueOrDefault(id)?.Name ?? $"#{id:D4}"));

            var lastSeen = chat.LastSeenMessageCount.GetValueOrDefault(accountId);
            var unreadCount = chat.Messages.Count - lastSeen;
            if (unreadCount < 0)
                unreadCount = 0;

            state.Chats.Add(new ChatEntry
            {
                ChatId = chatId,
                DisplayName = displayName,
                LastMessage = lastMsg,
                HasUnread = unreadCount > 0,
                UnreadCount = unreadCount,
            });

            if (chatId == currentChatId)
            {
                state.CurrentChat = BuildChatDetail(chat, accountId, serverComp, lastSeen);
                chat.LastSeenMessageCount[accountId] = chat.Messages.Count;
            }
        }
    }

    private static void DiscoverAccountsOnGrid(EntityUid discoveryContext, ChitterServerComponent server, ChitterHandlerDeps deps)
    {
        // Scoped to the owning station (not just the context entity's own grid) so PDAs on a docked
        // shuttle or an away-site grid that's still part of the station can still find contacts.
        var contextStation = deps.Station.GetOwningStation(discoveryContext);
        var entMan = deps.EntityManager;

        var query = entMan.EntityQueryEnumerator<ChitterAccountComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AccountId == 0)
                continue;

            if (contextStation != null && deps.Station.GetOwningStation(uid) != contextStation)
                continue;

            if (entMan.TryGetComponent<AccessComponent>(uid, out var access) && access.Tags.Contains("CentralCommand"))
                continue;

            // Not every Chitter account is an ID card - the station AI's own account lives directly on
            // its held entity, so fall back to the entity's own name/job instead of "Unknown".
            var jobTitle = entMan.TryGetComponent<IdCardComponent>(uid, out var idCard)
                ? idCard.LocalizedJobTitle ?? ""
                : entMan.HasComponent<ChitterAiComponent>(uid) ? Loc.GetString("job-name-station-ai") : "";
            var accountName = idCard?.FullName ?? entMan.GetComponent<MetaDataComponent>(uid).EntityName;
            deps.Server.RegisterOrUpdateAccount(server, comp.AccountId, accountName, jobTitle, comp.ProfilePictureId);
        }
    }

    public static ChatDetail BuildChatDetail(ChitterChat chat, uint ownId, ChitterServerComponent server, int lastSeen = 0)
    {
        var detail = new ChatDetail
        {
            ChatId = chat.ChatId,
            ChatName = chat.ChatName,
        };

        for (var i = 0; i < chat.Messages.Count; i++)
        {
            var msg = chat.Messages[i];
            var senderAcc = server.Accounts.GetValueOrDefault(msg.SenderAccountId);

            // Once emagged, even past messages read back with the current (fake) sender identity rather
            // than the real name that was baked in when they were sent - it's a live display error on
            // the compromised server, not a rewrite of history.
            var senderName = server.Emagged ? senderAcc?.Name ?? msg.SenderName : msg.SenderName;

            detail.Messages.Add(new MessageEntry
            {
                MessageId = msg.MessageId,
                SenderId = msg.SenderAccountId,
                SenderName = senderName,
                SenderProfilePicture = senderAcc?.ProfilePictureId ?? "",
                Timestamp = msg.Timestamp,
                Content = msg.Content,
                DeliveryFailed = msg.DeliveryFailed,
                IsOwn = msg.SenderAccountId == ownId,
                IsNew = i >= lastSeen,
            });
        }

        foreach (var pid in chat.ParticipantAccountIds)
        {
            var acc = server.Accounts.GetValueOrDefault(pid);
            detail.Participants.Add(new ParticipantEntry
            {
                AccountId = pid,
                Name = acc?.Name ?? $"#{pid:D4}",
                JobTitle = acc?.JobTitle ?? "",
                ProfilePictureId = acc?.ProfilePictureId ?? "",
            });
        }

        return detail;
    }
}
