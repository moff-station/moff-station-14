using System.Collections.Immutable;
using Content.Shared.Eui;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

[Serializable, NetSerializable]
public sealed class ChitterLogEuiState(
    IEnumerable<ChitterUiChatDetail> chats,
    IEnumerable<ChitterAccount> accounts
) : EuiStateBase
{
    public readonly ImmutableArray<ChitterUiChatDetail> Chats = chats.ToImmutableArray();
    public readonly ImmutableArray<ChitterAccount> Accounts = accounts.ToImmutableArray();
}
