using Content.Shared.Alert;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMagbootsSystem))]
public sealed partial class MagbootsComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> MagbootsAlert = "Magboots";

    /// <summary>
    /// If true, the user must be standing on a grid or planet map to experience the weightlessness-canceling effect
    /// </summary>
    [DataField]
    public bool RequiresGrid = true;

    // Moffstation - Start

    /// <summary>
    /// If true, check the toggle status of the parent entity to cancel weightlessness, <see cref="SharedMagbootsSystem"/>,
    /// otherwise use the EffectActive field
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseGenericToggle = true;

    /// <summary>
    /// Wheither or not the magboots are currently active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EffectActive = false;

    // Moffstation - End
}
