using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Silicons.StationAi;


public sealed class SharedStationAiMonitorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<StationAiWarpRequest>(OnStationAiWarpRequest);
    }

    public void RequestStationAiWarp(NetEntity uid, NetCoordinates target)
    {
        var request = new StationAiWarpRequest(uid, target);
        RaisePredictiveEvent(request);
    }

    private void OnStationAiWarpRequest(StationAiWarpRequest request)
    {
        var uid = GetEntity(request.Uid);

        if (!TryComp<EyeComponent>(uid, out var eye) || eye?.Target == null)
            return;

        if (!TryComp<StationAiMonitorComponent>(uid, out var comp) || comp?.NextWarpAllowed > _timing.CurTime)
            return;

        var xform = Transform(eye.Target.Value);
        if (xform.MapUid is null)
            return;

        _transform.SetCoordinates(
            eye.Target.Value,
            xform,
            new EntityCoordinates(
                _entityManager.GetEntity(request.TargetPosition.NetEntity),
                request.TargetPosition.Position));

        comp?.NextWarpAllowed = _timing.CurTime + TimeSpan.FromSeconds(comp.DelayBetweenWarps);
    }
}


[Serializable, NetSerializable]
public sealed class StationAiWarpRequest : EntityEventArgs
{
    public readonly NetEntity Uid;
    public readonly NetCoordinates TargetPosition;

    public StationAiWarpRequest(NetEntity uid, NetCoordinates targetPosition)
    {
        Uid = uid;
        TargetPosition = targetPosition;
    }
}

[Serializable, NetSerializable]
public enum StationAiMonitorUIKey
{
    Key,
}
