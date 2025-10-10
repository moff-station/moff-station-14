using System.Linq;
using Content.Shared._ES.Voting.Components;
using Content.Shared.EntityTable;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Voting;

/// <summary>
/// This handles in-game votes using <see cref="ESVoterComponent"/>
/// </summary>
public abstract partial class ESSharedVoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESVoteComponent, ComponentStartup>(OnVoteStartup);

        SubscribeLocalEvent<ESVoterComponent, PlayerAttachedEvent>(OnVoterPlayerAttached);
        SubscribeLocalEvent<ESVoterComponent, PlayerDetachedEvent>(OnVoterPlayerDetached);
        Subs.BuiEvents<ESVoterComponent>(ESVoterUiKey.Key,
            subs =>
            {
                subs.Event<ESSetVoteMessage>(OnSetVote);
            });

        InitializeOptions();
        InitializeResults();
    }

    private void OnVoteStartup(Entity<ESVoteComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;

        var ev = new ESGetVoteOptionsEvent();
        RaiseLocalEvent(ent, ref ev);
        DebugTools.Assert(ev.Options.Count > 0, $"Vote {ToPrettyString(ent)} has no options!");
        ent.Comp.Votes = ev.Options.Select(o => (o, new HashSet<NetEntity>())).ToDictionary();

        // Add a session override for all the present voters
        var query = EntityQueryEnumerator<ESVoterComponent, ActorComponent>();
        while (query.MoveNext(out _, out var actor))
        {
            _pvsOverride.AddSessionOverride(ent, actor.PlayerSession);
        }

        Dirty(ent);
    }

    private void OnVoterPlayerAttached(Entity<ESVoterComponent> ent, ref PlayerAttachedEvent args)
    {
        var query = EntityQueryEnumerator<ESVoteComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.AddSessionOverride(uid, args.Player);
        }
    }

    private void OnVoterPlayerDetached(Entity<ESVoterComponent> ent, ref PlayerDetachedEvent args)
    {
        var query = EntityQueryEnumerator<ESVoteComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.RemoveSessionOverride(uid, args.Player);
        }
    }

    private void OnSetVote(Entity<ESVoterComponent> ent, ref ESSetVoteMessage args)
    {
        if (!TryGetEntity(args.Vote, out var voteUid) ||
            !TryComp<ESVoteComponent>(voteUid, out var voteComp))
            return;

        // This vote doesn't contain this option.
        if (!voteComp.VoteOptions.Contains(args.Option))
            return;

        var voteNetEnt = GetNetEntity(ent);
        foreach (var (option, votes) in voteComp.Votes)
        {
            if (option.Equals(args.Option)) // add our vote
                votes.Add(voteNetEnt);
            else // clear our old votes
                votes.Remove(voteNetEnt);
        }
        Dirty(voteUid.Value, voteComp);
    }

    public void EndVote(Entity<ESVoteComponent> ent)
    {
        DebugTools.Assert(ent.Comp.Votes.Count > 0);
        var maxVote = ent.Comp.Votes.Values.Max(v => v.Count);

        // Handle ties gracefully
        var winningOptions = ent.Comp.Votes
            .Where(p => p.Value.Count == maxVote)
            .Select(p => p.Key)
            .ToList();

        // Random selection for tiebreak
        var result = _random.Pick(winningOptions);

        var ev = new ESVoteCompletedEvent(result);
        RaiseLocalEvent(ent, ref ev);

        SendVoteResultAnnouncement(ent, result);
        PredictedQueueDel(ent);
    }

    protected virtual void SendVoteResultAnnouncement(Entity<ESVoteComponent> ent, ESVoteOption result)
    {

    }

    public IEnumerable<Entity<ESVoteComponent>> EnumerateVotes()
    {
        var query = EntityQueryEnumerator<ESVoteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            yield return (uid, comp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESVoteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.EndTime)
                continue;
            EndVote((uid, comp));
        }
    }
}
