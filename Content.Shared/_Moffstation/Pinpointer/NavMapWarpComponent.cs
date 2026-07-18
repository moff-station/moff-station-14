using Content.Shared._Moffstation.Warp;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Pinpointer;

/// <summary>
/// This is used for entities that can warp using a navigation map interface <see cref="WarpComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavMapWarpComponent : Component;

/// <summary>
/// Raised on an entity to determine if it is able to warp via navigation map interface.
/// </summary>
/// <param name="Enabled"></param>
[Serializable, NetSerializable, ByRefEvent]
public record struct NavMapWarpEnabledQuery(bool Enabled);


/// <summary>
/// Raised on an entity to make it warp to a given location
/// </summary>
/// <param name="coordinates"></param>
[Serializable, NetSerializable]
public sealed class NavMapWarpRequest(NetCoordinates coordinates) : EntityEventArgs
{
    public readonly NetCoordinates Coordinates = coordinates;
}
