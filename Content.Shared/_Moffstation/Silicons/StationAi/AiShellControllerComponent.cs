using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Silicons.StationAi;

/// This component gives its entity actions and state necessary to inhabit and control AI shells.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(AiShellSystem))]
public sealed partial class AiShellControllerComponent : Component
{
    /// The action entity which toggles the shell control UI.
    [ViewVariables]
    public EntityUid? ToggleUiAction;

    /// The ID of the currently selected shell. This is stored so that UI state is preserved when the UI isn't open.
    [ViewVariables]
    public EntityUid? SelectedShell;

    /// All the shells that is available for this AI to control
    /// Updated whenever a Shell is created or destroyed, or when the Link to Core verb is used
    [DataField, AutoNetworkedField]
    public List<EntityUid> ControllableShells = new();

    /// The shell this controller is currently controlling.
    [DataField, AutoNetworkedField]
    public NetEntity? ControllingShell;

    /// The proto ID of <see cref="ToggleUiAction"/>.
    public static readonly EntProtoId<InstantActionComponent> ToggleUiActionProto = "ActionToggleAiShellControllerUi";
}
