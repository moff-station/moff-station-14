using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Warp;

/// <summary>
/// This is used for entities that can teleport themselves to an entity or a location.
/// </summary>
[RegisterComponent]
public sealed partial class WarpComponent : Component
{
    /// <summary>
    /// Minimum delay between two warps
    /// </summary>
    [DataField]
    public TimeSpan DelayBetweenWarps = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextAllowedWarp = TimeSpan.Zero;
}


/// <summary>
/// Base class to declare the target of a warp attempt
/// </summary>
[Serializable, NetSerializable]
public abstract class BaseWarpTarget;

/// <summary>
/// Declare an entity as a warp target
/// </summary>
/// <param name="entity"></param>
[Serializable, NetSerializable]
public sealed class EntityWarpTarget(NetEntity entity) : BaseWarpTarget
{
    public NetEntity Entity = entity;
}

/// <summary>
/// Declare coordinates as a warp target
/// </summary>
/// <param name="coordinates"></param>
[Serializable, NetSerializable]
public sealed class CoordinatesWarpTarget(NetCoordinates coordinates) : BaseWarpTarget
{
    public NetCoordinates Coordinates = coordinates;
}

/// <summary>
/// A client to server request to be warped to an entity
/// </summary>
[Serializable, NetSerializable]
public sealed class WarpRequestEvent(BaseWarpTarget target) : EntityEventArgs
{
    public BaseWarpTarget Target = target;
}

/// <summary>
/// A server to client response indicating the success or failure of the warp attempt
/// </summary>
[Serializable, NetSerializable]
public sealed class WarpResponseEvent(BaseWarpTarget target, bool success, string? failureCause) : EntityEventArgs
{
    /// <summary>
    /// The entity targeted by the warp attempt
    /// </summary>
    public BaseWarpTarget Target = target;

    /// <summary>
    /// Indicate the success or failure of the warp attempt
    /// </summary>
    public bool Success = success;

    /// <summary>
    /// If not null, explain the cause of the failure
    /// </summary>
    public string? FailureCause = failureCause;
}
