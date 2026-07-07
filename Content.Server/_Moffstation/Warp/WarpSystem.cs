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
            ! _entQuery.TryComp(ent, out var comp) ||
            _timing.CurTime < comp.NextAllowedWarp)
            return;

        var ev = new WarpAttemptEvent(ent, msg.Target);
        RaiseLocalEvent(ent, ref ev);

        if (!ev.Cancelled)
        {
            if (msg.Target is EntityWarpTarget entTarget)
                _xform.DropNextTo(ev.WarpedEntity, GetEntity(entTarget.Entity));
            if (msg.Target is CoordinatesWarpTarget coordTarget)
                _xform.SetCoordinates(ev.WarpedEntity, GetCoordinates(coordTarget.Coordinates));

            comp.NextAllowedWarp = _timing.CurTime + comp.DelayBetweenWarps;
        }

        RaiseLocalEvent(new WarpEvent(msg.Target, ! ev.Cancelled, ev.CancelReason));
    }
}


/// <summary>
/// Raised on the entity before a warp.
/// </summary>
[ByRefEvent]
public sealed class WarpAttemptEvent(EntityUid warpedEntity, BaseWarpTarget target) : CancellableEntityEventArgs
{
    /// <summary>
    /// Allow to override the entity that will be teleported to the target <see cref="EyeComponent"/>
    /// </summary>
    public EntityUid WarpedEntity = warpedEntity;

    /// <summary>
    /// Target location for the warp
    /// </summary>
    public readonly BaseWarpTarget Target = target;

    /// <summary>
    /// If not null contain a string explaining why the warp attempt failed
    /// </summary>
    public string? CancelReason;
}

/// <summary>
/// Raised on the entity after the warp attempt.
/// </summary>
public sealed class WarpEvent(BaseWarpTarget target, bool success, string? reason)
{
    /// <summary>
    /// Target location for the warp
    /// </summary>
    public readonly BaseWarpTarget Target = target;

    /// <summary>
    /// Indicate if the warp attempt was successful
    /// </summary>
    public bool Success = success;

    /// <summary>
    /// If not null contain a string explaining why the warp attempt failed
    /// </summary>
    public string? Reason = reason;
}
