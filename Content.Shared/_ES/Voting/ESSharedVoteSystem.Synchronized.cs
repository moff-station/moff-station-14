using System.Linq;
using Content.Shared._ES.Voting.Components;

namespace Content.Shared._ES.Voting;

public abstract partial class ESSharedVoteSystem
{
    private void InitializeSynchronized()
    {
        SubscribeLocalEvent<ESSynchronizedVoteManagerComponent, MapInitEvent>(OnSynchronizedMapInit);
        SubscribeLocalEvent<ESVoteCompletedEvent>(OnESVoteCompleted);
    }

    private void OnSynchronizedMapInit(Entity<ESSynchronizedVoteManagerComponent> ent, ref MapInitEvent args)
    {
        foreach (var proto in ent.Comp.Votes)
        {
            var voteEnt = Spawn(proto);
            ent.Comp.VoteEntities.Add(voteEnt);
            ent.Comp.Results.Add(null);

            // Moff Start - notify the vote entity now that it's registered in VoteEntities, so handlers
            // (e.g. enroll resolution) can walk back to this manager. Spawn() above ran the entity's own
            // MapInit synchronously, before this Add, so its MapInit is too early for that lookup.
            var spawnedEv = new ESVoteEntitySpawnedEvent(ent.Owner);
            RaiseLocalEvent(voteEnt, ref spawnedEv);
            // Moff end
        }
    }

    public void EndSynchronizedVotes(Entity<ESSynchronizedVoteManagerComponent> ent)
    {
        if (!ent.Comp.Completed)
            return;

        var results = ent.Comp.Results.Select(p => p!).ToList();
        var ev = new ESSynchronizedVotesCompletedEvent(results);
        RaiseLocalEvent(ent, ref ev);

        var postEv = new ESSynchronizedVotesPostCompletedEvent();
        RaiseLocalEvent(ent, ref postEv);
    }

    private void OnESVoteCompleted(ref ESVoteCompletedEvent args)
    {
        var query = EntityQueryEnumerator<ESSynchronizedVoteManagerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Completed)
                continue;
            if (!comp.VoteEntities.Contains(args.Vote))
                continue;
            var idx = comp.VoteEntities.IndexOf(args.Vote);
            comp.VoteEntities[idx] = EntityUid.Invalid;
            comp.Results[idx] = args.Result;
            Dirty(uid, comp);

            EndSynchronizedVotes((uid, comp));
            break;
        }
    }
}

// Moff Start - raised on a synchronized vote entity right after it's registered in its manager's
// VoteEntities, carrying the manager entity (which is also the game rule). Lets handlers resolve back to
// the manager at a point the vote entity's own MapInit is too early for.
[ByRefEvent]
public readonly record struct ESVoteEntitySpawnedEvent(EntityUid Manager);
// Moff end
