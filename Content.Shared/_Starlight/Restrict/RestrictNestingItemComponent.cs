using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Restrict;
[RegisterComponent, NetworkedComponent]
public sealed partial class RestrictNestingItemComponent : Component
{
    //doafter time
    [DataField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(5.0);
}
