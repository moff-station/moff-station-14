using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Pirate.Components;

/// <summary>
/// Tags grid as pirate shuttle
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PirateShuttleComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid AssociatedRule;
}
