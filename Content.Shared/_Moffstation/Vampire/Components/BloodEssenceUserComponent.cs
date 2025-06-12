using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodEssenceUserComponent : Component
{
    /// <summary>
    /// The vampire's current amount of blood essence collected.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodEssenceTotal = 0.0;

    [DataField]
    public List<ProtoId<ReagentPrototype>> BloodWhitelist = new();

    [DataField]
    public Dictionary<EntityUid, FixedPoint2> FedFrom = new();

}
