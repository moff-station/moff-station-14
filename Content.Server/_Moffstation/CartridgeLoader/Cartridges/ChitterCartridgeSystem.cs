using System.Linq;
using Content.Server._Moffstation.Chitter;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.IdentityManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.CartridgeLoader.Cartridges;

public sealed class ChitterCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private TimeSpan _nextRefresh = TimeSpan.Zero;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MessageCooldown = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ChitterCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + RefreshInterval;

        using (var query = EntityQueryEnumerator<CartridgeLoaderComponent>())
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

        switch (msg.Type)
        {
            case ChitterUiMessageType.NewChat:
                HandleNewChat(ent, loader, msg);
                break;
            case ChitterUiMessageType.SelectChat:
                HandleSelectChat(ent, msg);
                break;
            case ChitterUiMessageType.SendMessage:
                HandleSendMessage(ent, loader, msg);
                break;
            case ChitterUiMessageType.LeaveChat:
                HandleLeaveChat(ent, loader, msg);
                break;
            case ChitterUiMessageType.AddParticipant:
                HandleAddParticipant(ent, loader, msg);
                break;
            case ChitterUiMessageType.RemoveParticipant:
                HandleRemoveParticipant(ent, loader, msg);
                break;
            case ChitterUiMessageType.ArchiveChat:
                HandleArchiveChat(ent, loader, msg);
                break;
            case ChitterUiMessageType.SetProfilePicture:
                HandleSetProfilePicture(ent, loader, msg);
                break;
            case ChitterUiMessageType.RefreshContacts:
                // Triggers an immediate UI refresh via the UpdateUi at the end of OnMessage
                break;
            case ChitterUiMessageType.RenameChat:
                HandleRenameChat(ent, loader, msg);
                break;
        }

        UpdateUi(ent, loader, discoverContacts);
    }

    private bool TryGetServerAndCard(
        EntityUid loader,
        out Entity<ChitterServerComponent> serverEnt,
        out Entity<ChitterAccountComponent> card,
        bool requirePower = true)
    {
        serverEnt = default;
        card = default;

        var found = requirePower
            ? _server.TryFindServer(loader, out serverEnt)
            : _server.TryFindServerAnyPower(loader, out serverEnt);

        if (!found)
            return false;

        if (!_server.TryGetPdaIdCard(loader, out var idCard))
            return false;

        if (HasCentComAccess(idCard))
            return false;

        if (!TryComp<ChitterAccountComponent>(idCard, out var foundCard))
            return false;
        card = (idCard, foundCard);
        return true;
    }

    /// <summary>
    /// Gets the chat only if the given account is currently a participant of it. Guards every chat-mutating
    /// handler against acting on a chat the caller isn't part of.
    /// </summary>
    private bool TryGetParticipantChat(ChitterServerComponent server, Guid chatId, uint accountId, out ChitterChat chat)
    {
        chat = default!;

        // Only live chats are actionable; an archived chat should not accept renames, new
        // messages, or participant changes.
        if (!server.Chats.TryGetValue(chatId, out var foundChat))
            return false;

        if (!foundChat.ParticipantAccountIds.Contains(accountId))
            return false;

        chat = foundChat;
        return true;
    }

    private bool HasCentComAccess(EntityUid uid)
    {
        return TryComp<AccessComponent>(uid, out var access) && access.Tags.Contains("CentralCommand");
    }

    private string GetCardName(EntityUid idCard)
    {
        return TryComp<IdCardComponent>(idCard, out var idComp) && !string.IsNullOrEmpty(idComp.FullName)
            ? idComp.FullName
            : "Unknown";
    }

    private void HandleNewChat(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (_timing.CurTime < ent.Comp.NextMessageAllowed)
            return;

        var ownId = card.Comp.AccountId;

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

        ent.Comp.NextMessageAllowed = _timing.CurTime + MessageCooldown;

        var chatId = _server.CreateChat(serverEnt.Comp, participants, msg.ChatName);
        ent.Comp.CurrentChatId = chatId;
    }

    private void HandleSendMessage(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        // Resolve the server regardless of power so an already-unpowered server still gets its
        // message recorded and marked as failed, instead of the send silently doing nothing.
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card, requirePower: false))
            return;

        if (msg.ChatId == null || string.IsNullOrWhiteSpace(msg.Content))
            return;

        if (_timing.CurTime < ent.Comp.NextMessageAllowed)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, card.Comp.AccountId, out _))
            return;

        ent.Comp.NextMessageAllowed = _timing.CurTime + MessageCooldown;

        var senderName = GetCardName(card.Owner);
        _server.AddMessage(serverEnt.Comp, msg.ChatId.Value, card.Comp.AccountId, senderName, msg.Content);

        if (!_server.IsServerPowered(serverEnt))
            _server.MarkDeliveryFailed(serverEnt.Comp, msg.ChatId.Value);
    }

    private void HandleLeaveChat(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId != null)
            _server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, card.Comp.AccountId);
    }

    private void HandleAddParticipant(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId == null || msg.TargetNumber == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, card.Comp.AccountId, out _))
            return;

        _server.AddParticipantToChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private void HandleRemoveParticipant(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId == null || msg.TargetNumber == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, card.Comp.AccountId, out _))
            return;

        _server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private void HandleArchiveChat(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, card.Comp.AccountId, out _))
            return;

        _server.ArchiveChat(serverEnt.Comp, msg.ChatId.Value);
    }

    private void HandleRenameChat(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ChatId == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, card.Comp.AccountId, out _))
            return;

        _server.RenameChat(serverEnt.Comp, msg.ChatId.Value, msg.ChatName);
    }

    private void HandleSetProfilePicture(Entity<ChitterCartridgeComponent> ent, EntityUid loader, ChitterUiMessageEvent msg)
    {
        if (!TryGetServerAndCard(loader, out var serverEnt, out var card))
            return;

        if (msg.ProfilePictureId == null || !_prototypeManager.HasIndex<ChitterAvatarPrototype>(msg.ProfilePictureId))
            return;

        card.Comp.ProfilePictureId = msg.ProfilePictureId;
        Dirty(card);

        var ownerName = "Unknown";
        var ownerJobTitle = "Unknown";
        if (TryComp<IdCardComponent>(card.Owner, out var idCardComp))
        {
            ownerName = idCardComp.FullName ?? "Unknown";
            ownerJobTitle = idCardComp.LocalizedJobTitle ?? "Unknown";
        }

        _server.RegisterOrUpdateAccount(serverEnt.Comp, card.Comp.AccountId, ownerName, ownerJobTitle, msg.ProfilePictureId);
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
                var serverComp = serverEnt.Comp;
                _server.RegisterOrUpdateAccount(serverComp, account.AccountId, ownerName, ownerJobTitle, account.ProfilePictureId);

                // The grid-wide scan below is comparatively expensive; only run it on the periodic
                // refresh (or an explicit RefreshContacts request), not after every single message.
                if (discoverContacts)
                    DiscoverAccountsOnGrid(loader, serverComp);

                foreach (var (accId, acc) in serverComp.Accounts)
                {
                    if (accId == account.AccountId)
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
                    if (!chat.ParticipantAccountIds.Contains(account.AccountId))
                        continue;

                    var lastMsg = chat.Messages.Count > 0 ? chat.Messages[^1].Content : "";
                    var displayName = !string.IsNullOrWhiteSpace(chat.ChatName)
                        ? chat.ChatName
                        : string.Join(", ",
                            chat.ParticipantAccountIds
                                .Where(id => id != account.AccountId)
                                .Select(id => serverComp.Accounts.GetValueOrDefault(id)?.Name ?? $"#{id:D4}"));

                    var lastSeen = chat.LastSeenMessageCount.GetValueOrDefault(account.AccountId);
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

                    if (chatId == ent.Comp.CurrentChatId)
                    {
                        state.CurrentChat = BuildChatDetail(chat, account.AccountId, serverComp, lastSeen);
                        chat.LastSeenMessageCount[account.AccountId] = chat.Messages.Count;
                    }
                }
            }
        }

        _cartridge.UpdateCartridgeUiState(loader, state);
    }

    private void DiscoverAccountsOnGrid(EntityUid loader, ChitterServerComponent server)
    {
        // Scoped to the owning station (not just the loader's own grid) so PDAs on a docked
        // shuttle or an away-site grid that's still part of the station can still find contacts.
        var loaderStation = _station.GetOwningStation(loader);

        using (var query = EntityQueryEnumerator<ChitterAccountComponent>())
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AccountId == 0)
                continue;

            if (loaderStation != null && _station.GetOwningStation(uid) != loaderStation)
                continue;

            if (HasCentComAccess(uid))
                continue;

            var jobTitle = TryComp<IdCardComponent>(uid, out var idCard)
                ? idCard.LocalizedJobTitle ?? ""
                : "";
            var accountName = idCard?.FullName ?? "Unknown";
            _server.RegisterOrUpdateAccount(server, comp.AccountId, accountName, jobTitle, comp.ProfilePictureId);
        }
    }

    private void HandleSelectChat(Entity<ChitterCartridgeComponent> ent, ChitterUiMessageEvent msg)
    {
        if (msg.ChatId == null)
            return;

        ent.Comp.CurrentChatId = msg.ChatId;
    }

    private ChatDetail BuildChatDetail(ChitterChat chat, uint ownId, ChitterServerComponent server, int lastSeen = 0)
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
            detail.Messages.Add(new MessageEntry
            {
                MessageId = msg.MessageId,
                SenderId = msg.SenderAccountId,
                SenderName = msg.SenderName,
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
