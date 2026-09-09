using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Radio;

/// <summary>
/// This is used for entities that can warp to another entity location via the chat <see cref="WarpComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class RadioWarpComponent : Component;


/// <summary>
/// Raised on an entity to determine if it is able to warp via radio
/// </summary>
/// <param name="Enabled"></param>
[ByRefEvent]
public record struct RadioWarpEnabledQuery(bool Enabled);
