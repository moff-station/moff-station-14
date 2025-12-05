using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Can this entity be reflected.
/// Only applies if it is shot like a projectile and not if it is thrown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RicochetComponent : Component
{
    /// <summary>
    /// Probability for a projectile to ricochet (bounce ignoring structure reflection).
    /// </summary>
    [DataField("prob")]
    public float RicochetProb = 0.3f;

    /// <summary>
    /// Percentage by which ricochet probability drops after each ricochet.
    /// </summary>
    [DataField("dropoff")]
    public float RicochetDrop = 0.5f;

    /// <summary>
    /// The sound to play when reflecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));
}
