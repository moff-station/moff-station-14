using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Voting.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Voting.Systems;

public abstract partial class MoffSharedVoteEntrySystem : EntitySystem
{
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MoffVoteEntryComponent> ent, ref MapInitEvent args)
    {
        // Add a session override for all the present voters
        foreach (var voter in EntityQueryEnumerator<ESVoterComponent, ActorComponent>().AsEnumerable())
        {
            _pvsOverride.AddSessionOverride(ent, voter.Comp2.PlayerSession);
            _uiSystem.TryOpenUi(voter.Owner, ESVoterUiKey.Key, voter.Owner);
        }
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnVoterPlayerAttached(Entity<ESVoterComponent> ent, ref PlayerAttachedEvent args)
    {
        foreach (var entry in EntityQueryEnumerator<MoffVoteEntryComponent>().AsEnumerable())
        {
            _pvsOverride.AddSessionOverride(entry, args.Player);
        }
    }

    [SubscribeLocalEvent]
    private void OnVoterPlayerDetached(Entity<ESVoterComponent> ent, ref PlayerDetachedEvent args)
    {
        foreach (var entry in EntityQueryEnumerator<MoffVoteEntryComponent>().AsEnumerable())
        {
            _pvsOverride.RemoveSessionOverride(entry, args.Player);
        }
    }

    public IEnumerable<Entity<MoffVoteEntryComponent>> EnumerateEntries()
    {
        return EntityQueryEnumerator<MoffVoteEntryComponent>().AsEnumerable();
    }
}
