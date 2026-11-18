using Content.Server.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Remotes.Components;

/// <summary>
/// This is used for remotes able to deactivate surveillance machines from a distance.
/// It works by pulsing the Power and Ai vision of the device.
/// </summary>
[RegisterComponent]
public sealed partial class HackerRemoteComponent : Component
{
    /// <summary>
    /// Layouts on which this remote work.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<WireLayoutPrototype>> AcceptedLayouts = [];

    /// <summary>
    /// Maximum distance at which the remote can act on a device
    /// </summary>
    [DataField]
    public float Range = 20f;

    /// <summary>
    /// If true the remote can work even when the user cannot see the target
    /// </summary>
    [DataField]
    public bool IgnoreOcclusion = true;

    /// <summary>
    /// Sound played to the user when successfully pulsed a target.
    /// </summary>
    [DataField]
    public SoundSpecifier PulseSound = new SoundPathSpecifier("/Audio/_Moffstation/Machines/boobeep.ogg");
}
