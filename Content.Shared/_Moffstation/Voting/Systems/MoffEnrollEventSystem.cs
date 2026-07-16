using Content.Shared._Moffstation.Voting.Components;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Voting.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class MoffEnrollEventSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<MoffSetEnrollMessage>(OnSetEnroll);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MoffEnrollEventComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
        Dirty(ent);
    }

    private void OnSetEnroll(MoffSetEnrollMessage args, EntitySessionEventArgs ev)
    {
        if (ev.SenderSession.AttachedEntity is not { } attachedEntity)
            return;

        if (!TryGetEntity(args.Enroller, out var enrollerUid) ||
            !TryComp<MoffEnrollEventComponent>(enrollerUid, out var comp) ||
            !comp.Enrollable)
            return;

        var netAttached = GetNetEntity(attachedEntity);
        if (args.Enrolled)
            comp.Enrolled.Add(netAttached);
        else
            comp.Enrolled.Remove(netAttached);

        Dirty(enrollerUid.Value, comp);
    }
}
