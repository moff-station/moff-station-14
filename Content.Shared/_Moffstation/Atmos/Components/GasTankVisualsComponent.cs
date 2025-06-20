using Content.Shared._Moffstation.Atmos.EntitySystems;
using Content.Shared._Moffstation.Atmos.Visuals;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Atmos.Components;

/// <summary>
/// This component stores the <see cref="GasTankColorValues"/> which describe how the owning entity should look
/// (assuming it's a Gas Tank).
/// </summary>
[RegisterComponent, AutoGenerateComponentState, Access(typeof(GasTankVisualsSystem))]
public sealed partial class GasTankVisualsComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public GasTankColorValues Visuals = new(default);

    /// <summary>
    /// Entity prototypes don't specify colors directly, and instead reference predefined
    /// <see cref="GasTankVisualStylePrototype"/>s which contain the color values. This field is not used after the
    /// component is initialized.
    /// </summary>
    [DataField("visuals", readOnly: true)]
    public ProtoId<GasTankVisualStylePrototype> InitialVisuals = GasTankVisualStylePrototype.DefaultId;
}
