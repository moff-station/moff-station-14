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
