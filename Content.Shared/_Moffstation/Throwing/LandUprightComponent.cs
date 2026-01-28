using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Throwing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LandUprightComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Chance;
}
