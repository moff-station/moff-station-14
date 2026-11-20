using Content.Server.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Remotes;

/// <summary>
///     This is used to disable machine from a distance.
///     Work by pulsing the first Power wire we find.
/// </summary>
[RegisterComponent]
public sealed partial class HackingRemoteComponent : Component
{
    /// <summary>
    /// Layouts on which the remote will work.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<WireLayoutPrototype>> Layouts = [];

    /// <summary>
    /// Maximum range at which the remote can work.
    /// </summary>
    [DataField]
    public float MaxRange = 20f;

    /// <summary>
    /// If not null, sound played to the user after something is hacked.
    /// </summary>
    [DataField]
    public SoundSpecifier? BeepSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");
}
