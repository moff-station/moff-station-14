using Content.Shared.Power;
using Robust.Shared.GameStates;
using Content.Shared.Power.Components;

namespace Content.Shared._Moffstation.Power.Components;

/// This component causes powered machines (which aren't <see cref="SharedApcPowerReceiverComponent"/>!) to show their powered
/// state in examine messages and to reflect their powered state in <see cref="PowerDeviceVisuals"/> in appearance data.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerConsumerVisualsComponent : Component
{
    /// Whether or not this entity is receiving enough power to appear powered.
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Powered;
}
