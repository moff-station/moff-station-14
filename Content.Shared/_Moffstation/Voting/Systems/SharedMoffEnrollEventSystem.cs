using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Voting.Systems;

public abstract partial class SharedMoffEnrollEventSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESVoterComponent, MoffSetEnrollMessage>(OnSetEnroll);
        SubscribeLocalEvent<ESVoterComponent, MoffSetEnrollRandomMessage>(OnSetEnrollRandom);
    }

    private void OnSetEnroll(Entity<ESVoterComponent> ent, ref MoffSetEnrollMessage args)
    {
        // Ghosts only - assignment bypasses the mutual-antag check, so this gate is what stops an embodied
        // already-antag from getting force-assigned a second, exclusive one.
        if (!HasComp<GhostComponent>(args.Actor))
            return;

        if (!TryGetEntity(args.Enroller, out var enrollerUid) ||
            !TryComp<MoffEnrollEventComponent>(enrollerUid, out var comp))
            return;

        if (args.Enrolled)
        {
            comp.Enrolled.Add(args.Actor);
        }
        else
        {
            comp.Enrolled.Remove(args.Actor);
            // Their character choice goes with them, so re-enrolling starts from their own character again.
            comp.RandomPick.Remove(GetNetEntity(args.Actor));
        }

        Dirty(enrollerUid.Value, comp);
    }

    private void OnSetEnrollRandom(Entity<ESVoterComponent> ent, ref MoffSetEnrollRandomMessage args)
    {
        // Ghosts only, matching OnSetEnroll - a random-character pick is only meaningful for an enrollee.
        if (!HasComp<GhostComponent>(args.Actor))
            return;

        if (!TryGetEntity(args.Enroller, out var enrollerUid) ||
            !TryComp<MoffEnrollEventComponent>(enrollerUid, out var comp) ||
            !comp.CharacterSelection)
            return;

        var netAttached = GetNetEntity(args.Actor);
        if (args.Random)
            comp.RandomPick.Add(netAttached);
        else
            comp.RandomPick.Remove(netAttached);

        Dirty(enrollerUid.Value, comp);
    }
}
