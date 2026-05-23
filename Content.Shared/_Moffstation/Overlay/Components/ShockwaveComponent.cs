using Content.Shared._Moffstation.Overlay.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Overlay.Components;

/// <summary>
/// Holds the parameters for the shockwave overlay on an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedShockwaveSystem))]
public sealed partial class ShockwaveComponent : Component
{
    /// <summary>
    /// Intensity of the shockwave
    /// </summary>
    [DataField]
    public float Intensity = 10.0f;

    /// <summary>
    /// Range of the shockwave
    /// </summary>
    [DataField]
    public float Range = 5.0f;

    /// <summary>
    /// Rate of falloff from the epicenter
    /// </summary>
    [DataField]
    public float FallOff = 1.0f;

    /// <summary>
    /// Timescale value for the shader.
    /// </summary>
    [DataField]
    public float TimeScale = 0.5f;

    /// <summary>
    /// The start time for the shader.
    /// This is set by the Overlay when active.
    /// </summary>
    [DataField]
    public TimeSpan StartTime = TimeSpan.Zero;

    /// <summary>
    /// The duration for how long to let the shader play out.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2.0f);

    /// <summary>
    /// Whether this shockwave is active or not.
    /// </summary>
    [DataField]
    public bool Active = false;
}
