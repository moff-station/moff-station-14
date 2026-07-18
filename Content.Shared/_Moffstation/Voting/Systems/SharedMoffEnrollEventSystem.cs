using Content.Shared._Moffstation.Voting.Components;
using Content.Shared.Ghost;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Voting.Systems;

public abstract partial class SharedMoffEnrollEventSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<MoffSetEnrollMessage>(OnSetEnroll);
        SubscribeAllEvent<MoffSetEnrollRandomMessage>(OnSetEnrollRandom);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MoffEnrollEventComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
        Dirty(ent);
    }

    private void OnSetEnroll(MoffSetEnrollMessage args, EntitySessionEventArgs ev)
    {
        // Ghosts only - assignment bypasses the mutual-antag check, so this gate is what stops an embodied
        // already-antag from getting force-assigned a second, exclusive one.
        if (ev.SenderSession.AttachedEntity is not { } attachedEntity || !HasComp<GhostComponent>(attachedEntity))
            return;

        if (!TryGetEntity(args.Enroller, out var enrollerUid) ||
            !TryComp<MoffEnrollEventComponent>(enrollerUid, out var comp) ||
            !comp.Enrollable)
            return;

        var netAttached = GetNetEntity(attachedEntity);
        if (args.Enrolled)
        {
            comp.Enrolled.Add(netAttached);
        }
        else
        {
            comp.Enrolled.Remove(netAttached);
            // Their character choice goes with them, so re-enrolling starts from their own character again.
            comp.RandomPick.Remove(netAttached);
        }

        Dirty(enrollerUid.Value, comp);
    }

    private void OnSetEnrollRandom(MoffSetEnrollRandomMessage args, EntitySessionEventArgs ev)
    {
        // Ghosts only, matching OnSetEnroll - a random-character pick is only meaningful for an enrollee.
        if (ev.SenderSession.AttachedEntity is not { } attachedEntity || !HasComp<GhostComponent>(attachedEntity))
            return;

        if (!TryGetEntity(args.Enroller, out var enrollerUid) ||
            !TryComp<MoffEnrollEventComponent>(enrollerUid, out var comp) ||
            !comp.Enrollable ||
            !comp.CharacterSelection)
            return;

        var netAttached = GetNetEntity(attachedEntity);
        if (args.Random)
            comp.RandomPick.Add(netAttached);
        else
            comp.RandomPick.Remove(netAttached);

        Dirty(enrollerUid.Value, comp);
    }
}
