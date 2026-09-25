using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Ringer;

/// <summary>
/// Makes a PDA able to open store UIs when the ringtone is set to a secret code.
/// Traitors are told the code when greeted.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRingerSystem))]
public sealed partial class RingerUplinkComponent : Component
{
    /// <summary>
    /// Whether to show the toggle uplink button in PDA settings.
    /// </summary>
    [DataField]
    public bool Unlocked;

    /// <summary>
    /// Sound played when the uplink fails to open or locks
    /// </summary>
    /// <returns></returns>
    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");
}
