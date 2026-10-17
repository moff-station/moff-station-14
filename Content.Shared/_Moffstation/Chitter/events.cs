using Content.Shared._Moffstation.Extensions;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

/// This event is raised on entities with <see cref="ChitterIdentityComponent"/> to collect information from other
/// components which are used to inform assemble <see cref="ChitterAccount"/>.
/// eg. It gets name and title from the ID card component.
[ByRefEvent]
public record struct ChitterGetIdentityInfoEvent(string? Name, string? Title);

/// This <see cref="CartridgeMessageEvent"/> is used to wrap <see cref="ChitterMsg"/>, allowing
/// <see cref="ChitterCartridgeComponent"/>s to work with the same events as <see cref="IntrinsicChitterClientComponent"/>
[Serializable, NetSerializable]
public sealed partial class ChitterCartridgeMessageEvent(ChitterMsg inner) : CartridgeMessageEvent
{
    public readonly ChitterMsg Inner = inner;
}

/// The sealed supertype of all Chitter UI messages.
// ReSharper disable once ClassCannotBeInstantiated // ur wrong
[Serializable, NetSerializable]
public abstract partial class ChitterMsg : BoundUserInterfaceMessage, ISealedInheritance
{
    private ChitterMsg()
    {
        // Private constructor seals inheritance to nested classes.
    }

    /// Changes the client to the main chat view
    [Serializable, NetSerializable]
    public sealed partial class GoToMainView : ChitterMsg;

    /// Changes the client to the server selection view
    [Serializable, NetSerializable]
    public sealed partial class GoToServerSelection : ChitterMsg;

    /// Changes the client to the settings view
    [Serializable, NetSerializable]
    public sealed partial class GoToSettings : ChitterMsg;

    /// Changes the client to the new chat creation view
    [Serializable, NetSerializable]
    public sealed partial class GoToNewChat : ChitterMsg;

    /// Changes the currently viewed chat
    [Serializable, NetSerializable]
    public sealed partial class ChangeSelectedChat(ChitterChatId id) : ChitterMsg
    {
        public readonly ChitterChatId ChatId = id;
    }

    /// Changes the server that this client is connected to, or disconnects from all servers if null.
    [Serializable, NetSerializable]
    public sealed partial class ChooseConnectedServer(NetEntity? server) : ChitterMsg
    {
        public readonly NetEntity? Server = server;
    }

    /// Changes the avatar of the identity associated with this client
    [Serializable, NetSerializable]
    public sealed partial class SetOwnAvatar(ProtoId<ChitterAvatarPrototype> proto)
        : ChitterMsg
    {
        public readonly ProtoId<ChitterAvatarPrototype> Proto = proto;
    }

    /// Sends a message to the currently viewed chat
    [Serializable, NetSerializable]
    public sealed partial class SendMessage(ChitterChatId id, string content) : ChitterMsg
    {
        public readonly ChitterChatId ChatId = id;
        public readonly string Content = content;
    }

    /// Creates and opens a new chat
    [Serializable, NetSerializable]
    public sealed partial class CreateNewChat(
        IEnumerable<ChitterAccountId> accountIds,
        string chatName
    ) : ChitterMsg
    {
        public readonly HashSet<ChitterAccountId> AccountIds = [.. accountIds];
        public readonly string ChatName = chatName;
    }
}
