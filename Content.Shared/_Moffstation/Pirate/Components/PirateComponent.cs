using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Pirate.Components;

[RegisterComponent, NetworkedComponent]
/// <see href="https://youtu.be/i8ju_10NkGY">
public sealed partial class PirateComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "PirateFaction";
}
