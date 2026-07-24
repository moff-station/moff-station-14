using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Voting.Systems;

public sealed partial class MoffVoteEntrySystem : EntitySystem
{
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MoffVoteEntryComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ESVoterComponent, PlayerAttachedEvent>(OnVoterPlayerAttached);
        SubscribeLocalEvent<ESVoterComponent, PlayerDetachedEvent>(OnVoterPlayerDetached);
    }

    private void OnMapInit(Entity<MoffVoteEntryComponent> ent, ref MapInitEvent args)
    {
        // Add a session override for all the present voters
        var query = EntityQueryEnumerator<ESVoterComponent, ActorComponent>();
        while (query.MoveNext(out var uid, out _, out var actor))
        {
            _pvsOverride.AddSessionOverride(ent, actor.PlayerSession);
            _uiSystem.TryOpenUi(uid, ESVoterUiKey.Key, uid);
        }
        Dirty(ent);
    }

    private void OnVoterPlayerAttached(Entity<ESVoterComponent> ent, ref PlayerAttachedEvent args)
    {
        var query = EntityQueryEnumerator<MoffVoteEntryComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.AddSessionOverride(uid, args.Player);
        }
    }

    private void OnVoterPlayerDetached(Entity<ESVoterComponent> ent, ref PlayerDetachedEvent args)
    {
        var query = EntityQueryEnumerator<MoffVoteEntryComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.RemoveSessionOverride(uid, args.Player);
        }
    }

    public IEnumerable<Entity<MoffVoteEntryComponent>> EnumerateEntries()
    {
        var query = EntityQueryEnumerator<MoffVoteEntryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            yield return (uid, comp);
        }
    }
}
