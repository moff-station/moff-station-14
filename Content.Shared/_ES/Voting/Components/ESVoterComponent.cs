using Content.Shared._Moffstation.Voting.Systems;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Components;

/// <summary>
/// Marker component that denotes someone as being able to vote on <see cref="ESVoteComponent"/>.
/// Additionally used for networking vote entities without divulging them to all clients.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(ESSharedVoteSystem), typeof(MoffVoteNotificationSystem))]
public sealed partial class ESVoterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool NotificationsEnabled = true;

    /// <summary>
    /// The alert for showing whether notifications are enabled, and for toggling them.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> AlertId = "MoffVoteNotification";
}

[Serializable, NetSerializable]
public enum ESVoterUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ESSetVoteMessage(NetEntity vote, ESVoteOption option) : EntityEventArgs
{
    public NetEntity Vote = vote;
    public ESVoteOption Option = option;
}
