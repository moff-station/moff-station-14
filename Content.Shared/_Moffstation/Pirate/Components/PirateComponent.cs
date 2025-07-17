using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Pirate.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PirateComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField("zombieStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "Pirate";
}
