using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Pinpointer;

/// <summary>
/// This is used for entities that can teleport using a navigation map interface.
/// Only work for incorporeal entities such as ghosts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavMapWarpComponent : Component
{
    /// <summary>
    /// Time delay between two consecutive warps.
    /// </summary>
    [DataField]
    public TimeSpan DelayBetweenWarps = TimeSpan.FromSeconds(0.5f);

    public TimeSpan NextWarpAllowed = TimeSpan.Zero;
}
