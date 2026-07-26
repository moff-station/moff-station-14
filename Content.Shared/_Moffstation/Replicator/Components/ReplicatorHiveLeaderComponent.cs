using Content.Shared._Moffstation.Replicator.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Replicator.Components;

/// This component is used to track the "leader" of a group of replicators. This can either be a queen, when no nest
/// exists, or a nest. Its purpose is to track replicators which are associated with each other.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ReplicatorHiveLeaderSystem))]
public sealed partial class ReplicatorHiveLeaderComponent : Component
{
    /// The replicators associated with this hive leader.
    [DataField, AutoNetworkedField]
    public List<EntityUid> Members = [];
}
