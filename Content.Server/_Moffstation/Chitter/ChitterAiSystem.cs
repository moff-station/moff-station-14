using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Chitter;

// AI-side equivalent of ChitterCartridgeSystem: the AI's held entity carries a ChitterAccountComponent directly
// (auto-assigned a number by ChitterAccountSystem's MapInit handler, same as any ID card) and this system wires
// its intrinsic Chitter UI to the shared ChitterServerSystem API, without any PDA/ID card indirection.
public sealed partial class ChitterAiSystem : EntitySystem
{
    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private TimeSpan _nextRefresh = TimeSpan.Zero;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MessageCooldown = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChitterAiComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ChitterAiComponent, ChitterAiUiMessageEvent>(OnMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRefresh)
            return;

        _nextRefresh = _timing.CurTime + RefreshInterval;

        var query = EntityQueryEnumerator<ChitterAiComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateUi((uid, comp));
        }
    }

    private void OnUiOpened(Entity<ChitterAiComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnMessage(Entity<ChitterAiComponent> ent, ref ChitterAiUiMessageEvent msg)
    {
        var discoverContacts = msg.Type is ChitterUiMessageType.RefreshContacts;

        switch (msg.Type)
        {
            case ChitterUiMessageType.NewChat:
                HandleNewChat(ent, msg);
                break;
            case ChitterUiMessageType.SelectChat:
                HandleSelectChat(ent, msg);
                break;
            case ChitterUiMessageType.SendMessage:
                HandleSendMessage(ent, msg);
                break;
            case ChitterUiMessageType.LeaveChat:
                HandleLeaveChat(ent, msg);
                break;
            case ChitterUiMessageType.AddParticipant:
                HandleAddParticipant(ent, msg);
                break;
            case ChitterUiMessageType.RemoveParticipant:
                HandleRemoveParticipant(ent, msg);
                break;
            case ChitterUiMessageType.ArchiveChat:
                HandleArchiveChat(ent, msg);
                break;
            case ChitterUiMessageType.SetProfilePicture:
                HandleSetProfilePicture(ent, msg);
                break;
            case ChitterUiMessageType.RefreshContacts:
                // Triggers an immediate UI refresh via the UpdateUi at the end of OnMessage
                break;
            case ChitterUiMessageType.RenameChat:
                HandleRenameChat(ent, msg);
                break;
        }

        UpdateUi(ent, discoverContacts);
    }

    private bool TryGetServerAndAccount(
        EntityUid aiEntity,
        out Entity<ChitterServerComponent> serverEnt,
        out Entity<ChitterAccountComponent> account,
        bool requirePower = true)
    {
        serverEnt = default;
        account = default;

        var found = requirePower
            ? _server.TryFindServer(aiEntity, out serverEnt)
            : _server.TryFindServerAnyPower(aiEntity, out serverEnt);

        if (!found)
            return false;

        if (!TryComp<ChitterAccountComponent>(aiEntity, out var foundAccount))
            return false;

        account = (aiEntity, foundAccount);
        return true;
    }

    // Only returns the chat if accountId is actually a participant, mirroring ChitterCartridgeSystem.
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

    private void HandleNewChat(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account))
            return;

        if (_timing.CurTime < ent.Comp.NextChatAllowed)
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

        ent.Comp.NextChatAllowed = _timing.CurTime + MessageCooldown;

        var chatId = _server.CreateChat(serverEnt.Comp, participants, msg.ChatName);
        ent.Comp.CurrentChatId = chatId;
    }

    private static void HandleSelectChat(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (msg.ChatId == null)
            return;

        ent.Comp.CurrentChatId = msg.ChatId;
    }

    private void HandleSendMessage(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account, requirePower: false))
            return;

        if (msg.ChatId == null || string.IsNullOrWhiteSpace(msg.Content))
            return;

        if (_timing.CurTime < ent.Comp.NextMessageAllowed)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        ent.Comp.NextMessageAllowed = _timing.CurTime + MessageCooldown;

        _server.AddMessage(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, Name(ent), msg.Content);

        if (!_server.IsServerPowered(serverEnt))
            _server.MarkDeliveryFailed(serverEnt.Comp, msg.ChatId.Value);
    }

    private void HandleLeaveChat(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account))
            return;

        if (msg.ChatId != null)
            _server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId);
    }

    private void HandleAddParticipant(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account))
            return;

        if (msg.ChatId == null || msg.TargetNumber == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        _server.AddParticipantToChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private void HandleRemoveParticipant(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account))
            return;

        if (msg.ChatId == null || msg.TargetNumber == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        _server.RemoveParticipantFromChat(serverEnt.Comp, msg.ChatId.Value, msg.TargetNumber.Value);
    }

    private void HandleArchiveChat(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account))
            return;

        if (msg.ChatId == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        _server.ArchiveChat(serverEnt.Comp, msg.ChatId.Value);
    }

    private void HandleRenameChat(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account))
            return;

        if (msg.ChatId == null)
            return;

        if (!TryGetParticipantChat(serverEnt.Comp, msg.ChatId.Value, account.Comp.AccountId, out _))
            return;

        _server.RenameChat(serverEnt.Comp, msg.ChatId.Value, msg.ChatName);
    }

    private void HandleSetProfilePicture(Entity<ChitterAiComponent> ent, ChitterAiUiMessageEvent msg)
    {
        if (!TryGetServerAndAccount(ent, out var serverEnt, out var account))
            return;

        if (msg.ProfilePictureId == null || !ProtoMan.HasIndex<ChitterAvatarPrototype>(msg.ProfilePictureId))
            return;

        account.Comp.ProfilePictureId = msg.ProfilePictureId;
        Dirty(account);

        _server.RegisterOrUpdateAccount(serverEnt.Comp, account.Comp.AccountId, Name(ent), Loc.GetString(JobName), msg.ProfilePictureId);
    }

    private const string JobName = "job-name-station-ai";

    private void UpdateUi(Entity<ChitterAiComponent> ent, bool discoverContacts = true)
    {
        var hasAccount = TryComp<ChitterAccountComponent>(ent, out var account);
        var serverOnline = _server.TryFindServer(ent, out var serverEnt);

        var state = new ChitterUiState
        {
            HasIdCard = hasAccount,
            ServerOnline = serverOnline,
        };

        if (hasAccount && account != null)
        {
            var ownerName = Name(ent);
            var ownerJobTitle = Loc.GetString(JobName);

            state.OwnNumber = account.AccountId;
            state.OwnProfilePicture = account.ProfilePictureId;
            state.OwnName = ownerName;
            state.OwnJob = ownerJobTitle;

            if (serverOnline)
            {
                var serverComp = serverEnt.Comp;
                _server.RegisterOrUpdateAccount(serverComp, account.AccountId, ownerName, ownerJobTitle, account.ProfilePictureId);

                if (discoverContacts)
                    DiscoverAccountsOnGrid(ent, serverComp);

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

        _ui.SetUiState(ent.Owner, ChitterAiUiKey.Key, state);
    }

    private void DiscoverAccountsOnGrid(EntityUid aiEntity, ChitterServerComponent server)
    {
        // Scoped to the owning station, same as ChitterCartridgeSystem's version.
        var aiStation = _station.GetOwningStation(aiEntity);

        var query = EntityQueryEnumerator<ChitterAccountComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AccountId == 0)
                continue;

            if (aiStation != null && _station.GetOwningStation(uid) != aiStation)
                continue;

            if (TryComp<AccessComponent>(uid, out var access) && access.Tags.Contains("CentralCommand"))
                continue;

            var jobTitle = TryComp<IdCardComponent>(uid, out var idCard)
                ? idCard.LocalizedJobTitle ?? ""
                : HasComp<ChitterAiComponent>(uid) ? Loc.GetString(JobName) : "";
            var accountName = idCard?.FullName ?? Name(uid);
            _server.RegisterOrUpdateAccount(server, comp.AccountId, accountName, jobTitle, comp.ProfilePictureId);
        }
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
