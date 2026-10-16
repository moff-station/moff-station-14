using System.Collections.Immutable;
using System.Linq;
using Content.Shared._Moffstation.Extensions;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Chitter;

[ByRefEvent]
public readonly record struct DirtyChitterEvent(EntityUid Server, ImmutableHashSet<ChitterAccountId> Recipients);

// This part has the public methods for other systems to use.
public sealed partial class ChitterServerSystem
{
    private void UpdateRecipients(Entity<ChitterServerComponent> server, IEnumerable<ChitterAccountId> recipients)
    {
        var ev = new DirtyChitterEvent(server.Owner, [.. recipients]);
        RaiseLocalEvent(ref ev);
    }

    /// Returns all of the <see cref="ChitterServerComponent"/> the given client can find. Broadly, this means servers
    /// on the same grid which are powered.
    [PublicAPI] public IEnumerable<Entity<ChitterServerComponent>> GetDiscoverableServers(Entity<ChitterClientComponent> client)
    {
        if (Transform(client).GridUid is not { } grid)
            yield break;

        foreach (var entity in EntityQueryEnumerator<ChitterServerComponent, TransformComponent>())
        {
            if (entity.Comp1.Discoverable &&
                entity.Comp2.GridUid == grid &&
                IsFunctional((entity, entity)))
            {
                yield return entity;
            }
        }
    }

    public bool Connect(Entity<ChitterClientComponent> client, Entity<ChitterServerComponent> server)
    {
        if (_chitterClient.GetAccountOrNull(client) is { } accountComp &&
            _chitterIdentity.GetAccountOrNull(accountComp) is { } account)
        {
            RegisterOrUpdateAccount(server, account);
            return true;
        }

        return false;
    }

    /// Whether or not the given server is diegetically operational. If it doesn't have power, if it's broken, etc., it's
    /// not functional.
    [PublicAPI]
    public bool IsFunctional(Entity<ChitterServerComponent> server) => _powerReceiver.IsPowered(server.Owner);

    /// Gets all of the accounts known to this server.
    [PublicAPI]
    public IEnumerable<ChitterAccount> GetAllAccounts(Entity<ChitterServerComponent> server) =>
        server.Comp.Accounts.Values;

    /// Gets all of the chats on the given server.
    [PublicAPI]
    public IEnumerable<ChitterChat> GetAllChats(Entity<ChitterServerComponent> server) =>
        server.Comp.Chats.Values;

    /// <see cref="GetAllChats"/> which include the given account as a participant.
    [PublicAPI]
    public IEnumerable<ChitterChat> GetParticipatingChats(
        Entity<ChitterServerComponent> server,
        ChitterAccountId account
    ) => GetAllChats(server).Where(chat => chat.Participants.Contains(account));

    /// Retrieves the chat with <paramref name="id"/> on <paramref name="server"/> if <paramref name="account"/> is a
    /// participant in it.
    [PublicAPI]
    public ChitterChat? GetParticipatingChat(
        Entity<ChitterServerComponent> server,
        ChitterChatId id,
        ChitterAccountId account
    )
    {
        if (!server.Comp.Chats.TryGetValue(id, out var chat) ||
            !chat.Participants.Contains(account))
            return null;

        return chat;
    }

    /// Creates a new chat on this server with the given name and participants.
    [PublicAPI]
    public ChitterChatId CreateChat(
        Entity<ChitterServerComponent> server,
        IEnumerable<ChitterAccountId> participants,
        string? chatName = null
    )
    {
        var participantsTrunc = participants.Take(MaxChatParticipants).ToHashSet();
        var chitterChatId = ChitterChatId.Random();
        server.Comp.Chats[chitterChatId] = new ChitterChat(
            chitterChatId,
            chatName?.Truncate(ChatNameCharLimit) ?? "",
            participantsTrunc,
            Messages: [],
            _timing.CurTime
        );
        DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Chats));
        UpdateRecipients(server, participantsTrunc);
        return chitterChatId;
    }

    /// Adds a new message with the given details to the given chat. This is "sending" a message from the given sender.
    [PublicAPI]
    public bool AddMessage(
        Entity<ChitterServerComponent> server,
        ChitterChatId chatId,
        ChitterAccountId senderId,
        string content
    )
    {
        if (GetParticipatingChat(server, chatId, senderId) is not { } chat)
            return false;

        var message = new ChitterMessage(
            ChitterMessageId.Random(),
            senderId,
            _timing.CurTime,
            content.Truncate(MessageCharLimit)
        );

        chat.Messages.Add(message);
        _replay.RecordServerMessage(new ChitterMessageReplayRecord(chatId, message));

        DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Chats));
        UpdateRecipients(server, chat.Participants);
        return true;
    }

    /// Adds the given participant to the given chat.
    [PublicAPI]
    public void AddParticipantToChat(
        Entity<ChitterServerComponent> server,
        ChitterChatId chatId,
        ChitterAccountId toAdd,
        ChitterAccountId requestorId
    )
    {
        if (GetParticipatingChat(server, chatId, requestorId) is { } chat
            && chat.Participants.Add(toAdd))
        {
            DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Chats));
            UpdateRecipients(server, chat.Participants);
        }
    }

    /// Removes the given participant from the given chat.
    [PublicAPI]
    public void RemoveParticipantFromChat(
        Entity<ChitterServerComponent> server,
        ChitterChatId chatId,
        ChitterAccountId toRemove,
        ChitterAccountId requestorId
    )
    {
        if (GetParticipatingChat(server, chatId, requestorId) is { } chat && chat.Participants.Remove(toRemove))
        {
            DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Chats));
            UpdateRecipients(server, [toRemove, .. chat.Participants]);
        }
    }

    /// Gets the <see cref="ChitterAccount"/> on the given server with the given ID, or null if no such account exists.
    [PublicAPI]
    public ChitterAccount? GetAccount(Entity<ChitterServerComponent> server, ChitterAccountId accountId) =>
        server.Comp.Accounts.GetValueOrDefault(accountId);

    private void RegisterOrUpdateAccount(
        Entity<ChitterServerComponent> server,
        ChitterAccount account
    )
    {
        var id = account.Id;
        if (ProtoMan.Resolve(server.Comp.CurrentEmagEffect, out var effect))
        {
            var generatedName = string.Join(
                " ",
                ProtoMan.ResolveAll(effect.NameParts)
                    .Select(partOptions => partOptions.Values.GetWithWrappingIndex(id))
                    .OfType<string>()
                    .Select(locId => Loc.GetString(locId))
            );

            account = !generatedName.IsWhiteSpace() ? account with { Name = generatedName } : account;
            account = effect.JobTitle is { } tLoc ? account with { JobTitle = Loc.GetString(tLoc) } : account;
            account = effect.Avatars.GetValueWithWrappingIndex(id) is { } avatar
                ? account with { ProfilePictureId = avatar }
                : account;
        }

        server.Comp.Accounts[id] = account;
        DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Accounts));
        UpdateRecipients(server, [account.Id]);
    }

    /// Sets the profile picture / avatar for the given account to the given avatar proto.
    [PublicAPI]
    public bool SetProfilePicture(
        Entity<ChitterServerComponent> server,
        ChitterAccountId account,
        ProtoId<ChitterAvatarPrototype> proto
    )
    {
        if (!server.Comp.Accounts.TryGetValue(account, out var acc) ||
            !ProtoMan.Resolve(proto, out var avatar) ||
            avatar.NotSelectable ||
            ProtoMan.Resolve(server.Comp.CurrentEmagEffect, out var effect) &&
            effect.Avatars.Count > 0)
            return false;

        server.Comp.Accounts[account] = acc with { ProfilePictureId = avatar };
        DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Accounts));
        UpdateRecipients(server, [account]);

        return true;
    }

    /// Changes the name of the given chat to the given name.
    [PublicAPI]
    public void RenameChat(
        Entity<ChitterServerComponent> server,
        ChitterChatId id,
        string? newName,
        ChitterAccountId requestorId
    )
    {
        if (GetParticipatingChat(server, id, requestorId) is not { } chat)
            return;

        DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Chats));
        chat.Name = (newName ?? "").Truncate(ChatNameCharLimit);
        UpdateRecipients(server, chat.Participants);
    }
}

/// Extensions used only by this file. I suppose I could put them in the moffstation extensions, but I am too lazy to
/// deal with that.
static file class ChitterServerExts
{
    extension(string str)
    {
        public string Truncate(int length) => str[..Math.Min(length, str.Length)];
    }

    private static bool TryIndexWrapping<T>(IReadOnlyList<T> elements, int number, out T? ret)
    {
        if (elements.Count > 0)
        {
            ret = elements[number % elements.Count];
            return true;
        }

        ret = default;
        return false;
    }


    extension<T>(IReadOnlyList<T> list) where T : struct
    {
        public T? GetValueWithWrappingIndex(ChitterAccountId id) =>
            TryIndexWrapping(list, id.Id, out var ret) ? ret : null;
    }

    extension<T>(IReadOnlyList<T> list) where T : class
    {
        public T? GetWithWrappingIndex(ChitterAccountId id) => TryIndexWrapping(list, id.Id, out var ret) ? ret : null;
    }
}
