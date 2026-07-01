using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Radio;

/// <summary>
/// This is used for entities that can teleport to another entity location via the chat window
/// </summary>
[RegisterComponent]
public sealed partial class RadioWarpComponent : Component
{
    /// <summary>
    /// Indicate if the entity can teleport to an entity location when their sensors are not set to coordinates.
    /// </summary>
    [DataField]
    public bool RequireCoordinates = true;

    /// <summary>
    /// Minimum delay between consecutive radio warps
    /// </summary>
    [DataField]
    public TimeSpan DelayBetweenWarps =  TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextAvailableWarp =  TimeSpan.Zero;
}



