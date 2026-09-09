using Content.Shared.Actions.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Replicator.Components;

/// This component makes an entity into a replicator. Replicators are members of a <see cref="HiveLeader"/>, and can be
/// upgraded from one entity to another at the direction of the hive leader.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReplicatorComponent : Component
{
    /// The entity with <see cref="ReplicatorHiveLeaderComponent"/> which this replicator follows.
    [DataField, AutoNetworkedField]
    public EntityUid? HiveLeader;


    /// Actions this replicator can use once it receives <see cref="EnableReplicatorUpgradesEvent"/>.
    [DataField]
    public HashSet<EntProtoId<ActionComponent>> UpgradeOptionActions = [];

    /// The instantiated entities from <see cref="UpgradeOptionActions"/>
    [ViewVariables(VVAccess.ReadOnly)]
    public readonly List<EntityUid> UpgradeActionEntities = [];


    /// Popup shown to self when upgrading is enabled
    [DataField]
    public LocId? UpgradeReadyPopup;

    /// Popup shown to self when upgrading to this replicator.
    [DataField]
    public LocId? UpgradedPopupSelf;

    /// Popup shown to others when upgrading to this replicator.
    [DataField]
    public LocId? UpgradedPopupOther;

    /// Sound to play when upgrading to this replicator.
    [DataField]
    public SoundSpecifier? UpgradeSound = new SoundPathSpecifier("/Audio/_Impstation/Misc/replicator_sfx2.ogg");


    /// Popup shown to self when a new leader is assigned.
    [DataField]
    public LocId? NewLeaderPopup;

    /// Popup shown to self when our owning nest is destroyed.
    [DataField]
    public LocId? NestDestroyedPopup;

    /// If its nest is destroyed, this replicator will upgrade into this prototype. If null, replicators will not
    /// upgrade on nest destruction.
    [DataField]
    public EntProtoId<ReplicatorComponent>? UpgradeToOnNestDestruction;

    /// Popup shown to self when our owning queen is killed.
    [DataField]
    public LocId? QueenKilledPopup;
}
