using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.AacTablet;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(AacTabletSystem))]
public sealed partial class AacTabletComponent : Component
{
    /// Delay between sending phrases.
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);
}
