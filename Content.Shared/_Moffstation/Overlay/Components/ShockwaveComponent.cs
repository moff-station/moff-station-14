using Content.Shared._Moffstation.Overlay.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Overlay.Components;

/// <summary>
/// Holds the parameters for the shockwave overlay on an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedShockwaveSystem))]
public sealed partial class ShockwaveComponent : Component
{
    /// <summary>
    /// Intensity of the shockwave.
    /// </summary>
    [DataField]
    public float Intensity = 10.0f;

    /// <summary>
    /// The width of the shockwave as it expands outward.
    /// </summary>
    [DataField]
    public float Width = 0.05f;

    /// <summary>
    /// Rate of falloff from the epicenter.
    /// </summary>
    [DataField]
    public float FallOff = 40.0f;

    /// <summary>
    /// Power factor for the distance offset.
    /// </summary>
    [DataField]
    public float PowerFactor = 0.8f;

    /// <summary>
    /// Time scale to slow down or speed up the shockwave
    /// </summary>
    [DataField]
    public float TimeScale = 1.0f;

    /// <summary>
    /// Start time for when the shader starts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StartTime = TimeSpan.Zero;
}
