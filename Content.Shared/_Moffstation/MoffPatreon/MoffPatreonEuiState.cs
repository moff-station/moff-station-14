using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.MoffPatreon;

[Serializable, NetSerializable]
public sealed class MoffPatreonEuiState : EuiStateBase
{
    public required List<UsernameAndGuid> Patrons { get; init; }
    public required List<string> OnlinePlayers { get; init; }
}

[Serializable, NetSerializable]
public readonly record struct UsernameAndGuid(string Username, Guid Id);

[Serializable, NetSerializable]
public sealed class MoffPatreonAddRequest : EuiMessageBase
{
    public required string UsernameOrId { get; init; }
}

[Serializable, NetSerializable]
public sealed class MoffPatreonRemoveRequest : EuiMessageBase
{
    public required Guid UserId { get; init; }
}

[Serializable, NetSerializable]
public sealed class MoffPatreonAddResponse : EuiMessageBase
{
    public required int Successful;
    public required List<string> Failed;
}

[Serializable, NetSerializable]
public sealed class MoffPatreonRemoveResponse : EuiMessageBase
{
    public required bool Success;
}
