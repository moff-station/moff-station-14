using Content.Shared._Moffstation.Warp;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Warp;

/// <summary>
/// This handles entities that can teleport to either an entity or a location (<see cref="WarpComponent"/>)
/// </summary>
public sealed partial class WarpSystem : SharedWarpSystem
{
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<WarpComponent> _entQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<WarpRequestEvent>(OnWarpRequest);
    }

    private void OnWarpRequest(WarpRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent ||
            !_entQuery.TryComp(ent, out var comp) ||
            _timing.CurTime < comp.NextAllowedWarp)
            return;

        var ev = new WarpAttemptEvent(ent, msg.Target, false, null);
        RaiseLocalEvent(ent, ref ev);

        if (!ev.Cancelled)
        {
            if (msg.Target is EntityWarpTarget entTarget)
                _xform.DropNextTo(ev.WarpedEntity, GetEntity(entTarget.Entity));
            if (msg.Target is CoordinatesWarpTarget coordTarget)
                _xform.SetCoordinates(ev.WarpedEntity, GetCoordinates(coordTarget.Coordinates));

            comp.NextAllowedWarp = _timing.CurTime + comp.DelayBetweenWarps;
        }

        var wrp = new WarpEvent(msg.Target, !ev.Cancelled, ev.CancelReason);
        RaiseLocalEvent(ent, ref wrp);
    }
}

/// <summary>
/// Raised on the entity attempting a warp.
/// WarpedEntity allow to override the entity teleported to the target <see cref="EyeComponent"/>.
/// Setting Cancelled to true will make the entity fail their attempt.
/// If not null, CancelReason contain a Loc string explaining the cause of the attempt failure.
/// </summary>
[ByRefEvent]
public record struct WarpAttemptEvent(EntityUid WarpedEntity, BaseWarpTarget Target, bool Cancelled, LocId? CancelReason);

/// <summary>
/// Raised on the entity after the warp attempt.
/// </summary>
[ByRefEvent]
public readonly record struct WarpEvent(BaseWarpTarget Target, bool Success, LocId? Reason);
