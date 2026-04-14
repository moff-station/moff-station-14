using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Silicons.StationAi;

/// This marker component allows its owner to be possessed by actors with the <see cref="AiShellControllerComponent"/>.
[RegisterComponent, NetworkedComponent, Access(typeof(AiShellSystem))]
public sealed partial class AiShellComponent : Component;

/// This component is applied to entities with <see cref="AiShellComponent"/> while they are actively being controlled.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(AiShellSystem))]
public sealed partial class ControlledAiShellComponent : Component
{
    /// Action entity for terminating a shell control session.
    [ViewVariables]
    public EntityUid? StopControlAction;

    /// The entity with <see cref="AiShellControllerComponent"/> which is currently controlling this shell.
    [DataField, AutoNetworkedField]
    public NetEntity Controller;

    /// The prototype to use to populate <see cref="StopControlAction"/>.
    public static readonly EntProtoId<InstantActionComponent> StopControlActionProto = "ActionStopAiShellControl";
}

/// This component indicates that its owner contains an entity with <see cref="AiShellComponent"/> in a container
/// which is "active" for that brain, eg. a brain in a borg's brain slot, as opposed that brain in a toolbox.
[RegisterComponent, NetworkedComponent, Access(typeof(AiShellSystem))]
public sealed partial class AiShellHolderComponent : Component
{
    /// Action entity for terminating a shell control session.
    [ViewVariables]
    public EntityUid? StopControlAction;

    /// The name to use for holders which is not currently being controlled.
    public static readonly LocId EmptyHolderName = "ai-shell-brain-empty";
}
