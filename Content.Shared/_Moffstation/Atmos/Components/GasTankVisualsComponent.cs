using Content.Shared._Moffstation.Atmos.EntitySystems;
using Content.Shared._Moffstation.Atmos.Visuals;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Atmos.Components;

[RegisterComponent, AutoGenerateComponentState, Access(typeof(GasTankVisualsSystem))]
public sealed partial class GasTankVisualsComponent : Component
{
    [DataField(tag: "appearance"), AutoNetworkedField]
    public GasTankColorValues Visuals = new(default);

    [DataField("visuals", required: true)]
    public ProtoId<GasTankVisualStylePrototype> InitialVisuals;
}
