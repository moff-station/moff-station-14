using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Replicator.Components;

/// This component makes its owner a replicator queen, capable of making a new nest.
[RegisterComponent, NetworkedComponent, Access(typeof(Systems.ReplicatorQueenSystem))]
public sealed partial class ReplicatorQueenComponent : Component
{
    /// The action that spawns a new nest.
    [DataField]
    public EntProtoId<InstantActionComponent> SpawnNewNestAction = "ActionReplicatorSpawnNest";

    /// The instantiated <see cref="SpawnNewNestAction"/> action entity.
    [ViewVariables]
    public EntityUid? SpawnNestActionEnt;

    /// The replicator prototype to upgrade to after creating a new nest.
    [DataField]
    public EntProtoId<ReplicatorComponent> UpgradeToAfterNestCreation = "MobReplicatorTier1";
}

[RegisterComponent, NetworkedComponent, Access(typeof(Systems.ReplicatorQueenSystem))]
public sealed partial class ReplicatorQueenSignComponent : Component
{
    /// The sprite layer to add to a queen's sprite. This is used to visually differentiate the replicator queen.
    [DataField]
    public PrototypeLayerData Sprite = new()
    {
        RsiPath = "_Impstation/Mobs/Replicator/replicator_sign.rsi",
        State = "sign",
    };
}
