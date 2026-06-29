using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Radio;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class SharedRadioWarpSystem : EntitySystem
{
    [Dependency] IGameTiming _time = default!;
    [Dependency] SharedSuitSensorSystem _sensor = default!;
    [Dependency] SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeAllEvent<RadioWarpRequest>(OnRadioWarpRequest);
        SubscribeLocalEvent<RadioWarpComponent, RadioWarpEnabledQuery>(OnRadioWarpEnabledQuery);
    }

    private void OnRadioWarpRequest(RadioWarpRequest request, EntitySessionEventArgs session)
    {
        var uid = GetEntity(request.Uid);
        var target = GetEntity(request.Target);

        if (!Exists(uid) || !Exists(target) || session.SenderSession.AttachedEntity != uid)
            return;

        if (!TryComp<RadioWarpComponent>(uid, out var warpComp) || warpComp.NextAvailableWarp > _time.CurTime)
            return;

        if (warpComp.RequireCoordinates && _sensor.GetSensorMode(target) != SuitSensorMode.SensorCords)
            return;

        if (TryComp<EyeComponent>(uid, out var eye) && eye.Target is not null)
            uid = eye.Target.Value;


        warpComp.NextAvailableWarp = _time.CurTime + warpComp.DelayBetweenWarps;
        _transform.SetWorldPosition(uid, _transform.GetWorldPosition(target));
    }

    private void OnRadioWarpEnabledQuery(Entity<RadioWarpComponent> ent, ref RadioWarpEnabledQuery ev)
    {
        ev.Enabled = true;
    }

    # region public API

    public void TryWarp(EntityUid ent, NetEntity target)
    {
        RaisePredictiveEvent(new RadioWarpRequest(GetNetEntity(ent), target));
    }
    # endregion
}

[ByRefEvent]
public record struct RadioWarpEnabledQuery(bool Enabled);

[Serializable, NetSerializable]
public sealed class RadioWarpRequest(NetEntity uid, NetEntity target) : EntityEventArgs
{
    public readonly NetEntity Uid = uid;
    public readonly NetEntity Target = target;
}
