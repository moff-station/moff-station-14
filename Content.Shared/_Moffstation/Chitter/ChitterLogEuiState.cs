using System.Collections.Immutable;
using Content.Shared.Eui;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

[Serializable, NetSerializable]
public sealed class ChitterLogEuiState(
    ChitterChatId? selectedChat,
    IEnumerable<ChitterUiChatDetail> chats,
    IEnumerable<ChitterAccount> accounts
) : EuiStateBase
{
    public readonly ChitterChatId? SelectedChat = selectedChat;
    public readonly ImmutableArray<ChitterUiChatDetail> Chats = chats.ToImmutableArray();
    public readonly ImmutableArray<ChitterAccount> Accounts = accounts.ToImmutableArray();
}

[Serializable, NetSerializable]
public sealed partial class ChitterLogEuiChatSelectedMessage(ChitterChatId? chat) : EuiMessageBase
{
    public readonly ChitterChatId? Chat = chat;
}
