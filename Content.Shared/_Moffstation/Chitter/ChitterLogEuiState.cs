using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

[Serializable, NetSerializable]
public sealed class ChitterLogEuiState : EuiStateBase
{
    public required List<ChitterLogChat> Chats { get; init; }
}

[Serializable, NetSerializable]
public sealed class ChitterLogChat
{
    public required Guid ChatId { get; init; }
    public required string ChatName { get; init; }
    public required bool Archived { get; init; }
    public required TimeSpan CreatedTime { get; init; }
    public required List<ChitterLogParticipant> Participants { get; init; }
    public required List<ChitterMessage> Messages { get; init; }
}

[Serializable, NetSerializable]
public sealed class ChitterLogParticipant
{
    public required uint AccountId { get; init; }
    public required string Name { get; init; }
}
