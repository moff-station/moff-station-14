using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Pinpointer;

/// <summary>
/// This handles teleportations from a navigation map interface.
/// </summary>
public sealed class SharedNavMapWarpSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeAllEvent<NavMapWarpRequest>(OnNavMapWarpRequest);
        SubscribeAllEvent<NavMapWarpEnabledQuery>(OnNavMapEnabledQuery);
    }

    private void OnNavMapWarpRequest(NavMapWarpRequest req)
    {
        var uid = GetEntity(req.Uid);

        if (!TryComp<NavMapWarpComponent>(uid, out var warpComp) || _time.CurTime < warpComp.NextWarpAllowed)
            return;

        if (TryComp<EyeComponent>(uid, out var eye) && eye.Target is not null)
            uid = eye.Target.Value;

        warpComp.NextWarpAllowed = _time.CurTime + warpComp.DelayBetweenWarps;

        _transform.SetCoordinates(
            uid,
            Transform(uid),
            GetCoordinates(req.Coordinates));
    }

    private void OnNavMapEnabledQuery(NavMapWarpEnabledQuery req)
    {
        req.Enabled = HasComp<NavMapWarpComponent>(GetEntity(req.Uid));
    }
}

[Serializable, NetSerializable]
public sealed class NavMapWarpRequest(NetEntity uid, NetCoordinates coordinates) : EntityEventArgs
{
    public readonly NetEntity Uid = uid;
    public readonly NetCoordinates Coordinates = coordinates;
}

[Serializable, NetSerializable]
public sealed class NavMapWarpEnabledQuery(NetEntity uid) : EntityEventArgs
{
    public readonly NetEntity Uid = uid;
    public bool Enabled = false;
}
