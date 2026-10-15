using Content.Shared._Harmony.BloodBrothers.EntitySystems;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.BloodBrothers.Components;

/// <summary>
/// Marks an entity that initial blood brothers can convert. Only sent to initial blood brothers.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBloodBrotherSystem))]
public sealed partial class BloodBrotherConvertibleComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> Icon = "BloodBrotherConvertible";

    public override bool SessionSpecific => true;
}
