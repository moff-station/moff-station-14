using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.JestographicSequencer;

/// <summary>
///Added by a jestographic sequencer to a recycler. Mobs that fall into it are dressed in "Outfit".
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JestographicRecyclerComponent : Component
{
    /// <summary>
    ///Inventory slot name to the unremovable item put in it.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> Outfit = new();

    /// <summary>
    ///How long of a stun going through this is.
    /// </summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1.0);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");
}
