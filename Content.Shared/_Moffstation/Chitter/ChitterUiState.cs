using System.Collections.Immutable;
using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Speech.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

// All of the types in this file are part of Chitter UI state. They should be immutable data that's serializable
// to be sent to clients.

/// The <see cref="BoundUserInterfaceState"/> for chitter. Contains only one <see cref="IChitterUiData"/>.
[Serializable, NetSerializable]
public sealed class ChitterUiState(IChitterUiData data) : BoundUserInterfaceState
{
    public IChitterUiData Data = data;
}

/// <summary>
/// A bundle of information about <see cref="ChitterAccount"/>s visible to a particular account.
/// </summary>
/// <param name="AllContacts">All contacts known to our connected server, including blocked contacts and ourselves</param>
/// <param name="Self">Our own account info</param>
[Serializable, NetSerializable]
public readonly record struct ChitterContacts(
    ImmutableArray<ChitterAccount> AllContacts,
    ChitterAccount Self
)
{
    /// <see cref="AllContacts"/>, minus blocked and minus self.
    public IEnumerable<ChitterAccount> WithoutSelf()
    {
        var selfId = Self.Id;
        return AllContacts.Where(it => it.Id != selfId);
    }

    /// <see cref="AllContacts"/>, minus blocked and minus self.
    public IEnumerable<ChitterAccount> WithSelf() => [Self, .. WithoutSelf()];
}

/// This interface defines the possible UI states for chitter.
// [RequiresSerializable] // Can't apply to interfaces, apparently.
public interface IChitterUiData : ISealedInheritance
{
    /// A UI with no driving identity,, should show an error to the user.
    [Serializable, NetSerializable]
    readonly record struct NoIdentity : IChitterUiData;

    /// A UI with an identity, but no connected server. Should show an error to the user while still giving access to
    /// account info.
    [Serializable, NetSerializable]
    readonly record struct NoServer(
        ChitterAccount Self
    ) : IChitterUiData;

    /// <summary>
    /// The normal chitter UI state. Contains our own account info as well as info about chats we're part of and
    /// contacts we can access.
    /// </summary>
    /// <param name="Chats">The chats we're a part of</param>
    /// <param name="CurrentChat">The chat we're currently looking at</param>
    /// <param name="Contacts">This client's contacts</param>
    [Serializable, NetSerializable]
    readonly record struct Ok(
        (NetEntity, string ServerName) ConnectedServer,
        ImmutableArray<ChitterUiChatEntry> Chats,
        ChitterUiChatDetail? CurrentChat,
        ChitterContacts Contacts
    ) : IChitterUiData;

    /// Chitter UI state where the Server Selection subwindow is open, presenting the contained
    /// <see cref="AvailableServers"/> as options to connect to.
    [Serializable, NetSerializable]
    readonly record struct ServerSelection(
        ImmutableArray<(string Name, NetEntity)> AvailableServers,
        NetEntity? CurrentServer
    ) : IChitterUiData;

    /// Chitter UI state where the Settings subwindow is open, allowing the user to modify their own account info.
    [Serializable, NetSerializable]
    readonly record struct Settings(ChitterAccount Self) : IChitterUiData;

    /// Chitter UI state where the New Chat subwindow is open, allowing users to create a new chat with participants
    /// from the contained <see cref="Contacts"/>.
    [Serializable, NetSerializable]
    readonly record struct NewChat(ChitterContacts Contacts) : IChitterUiData;
}

/// Info about chats we're not necessarily looking at right now. This is enough to populate a pane to select the
/// chat from.
[Serializable, NetSerializable]
public readonly record struct ChitterUiChatEntry(
    ChitterChatId Id,
    string Title,
    string? Preview
)
{
    /// Creates a <see cref="ChitterUiChatEntry"/> for the given <see cref="ChitterChat"/>.
    public static ChitterUiChatEntry CreateFor(
        ChitterChat chat,
        ChitterAccount self,
        InfallibleChitterAccountLookup accounts
    )
    {
        string name;
        if (chat.Name.IsWhiteSpace())
        {
            var participantsExceptSelf = chat.Participants.Where(it => it != self.Id)
                .Select(it => accounts(it).Name)
                .Take(1) // There is so little space, we can really only deal with one name.
                .ToList();
            var numOtherParticipants = chat.Participants.Count - 1;
            name = numOtherParticipants - participantsExceptSelf.Count is var diff && diff > 0
                ? Loc.GetString(
                    "chitter-client-participants-truncated",
                    ("base", string.Join(", ", participantsExceptSelf)),
                    ("additional", diff)
                )
                : string.Join(", ", participantsExceptSelf);
        }
        else
        {
            name = chat.Name;
        }

        return new ChitterUiChatEntry(
            chat.Id,
            name,
            chat.Messages.Count > 0 ? chat.Messages[^1].Content : null
        );
    }
}

/// Detailed info about a single chitter chat, including its messages.
[Serializable, NetSerializable]
public readonly record struct ChitterUiChatDetail(
    ChitterChatId Id,
    string Name,
    TimeSpan CreatedTime,
    ImmutableArray<ChitterUiMessage> Messages,
    ImmutableArray<ChitterAccountId> Participants
)
{
    public static ChitterUiChatDetail From(ChitterChat chat) => new(
        chat.Id,
        chat.Name,
        chat.CreatedTime,
        chat.Messages.Select(ChitterUiMessage.From).ToImmutableArray(),
        chat.Participants.ToImmutableArray()
    );
}

/// Info about a single chitter message. Really, this is just a readonly <see cref="ChitterMessage"/>.
[Serializable, NetSerializable]
public readonly record struct ChitterUiMessage(
    ChitterMessageId Id,
    ChitterAccountId Sender,
    TimeSpan SentAt,
    string Content
)
{
    public static ChitterUiMessage From(ChitterMessage chat) =>
        new(chat.Id, chat.Sender, chat.Timestamp, chat.Content);
}
