using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Mobs;

/// <summary>
/// This is used for entities that become Mobs when waken up and return to inanimate items when they die.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobWhenAliveComponent : Component
{
    /// <summary>
    /// Component added when the mob become alive
    /// and removed when the mob die.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry AddWhenAlive = new();
}
