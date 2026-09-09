using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._Moffstation.Voting.Systems;

public abstract partial class MoffSharedVoteEntrySystem : EntitySystem
{
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MoffVoteEntryComponent> ent, ref MapInitEvent args)
    {
        // Add a session override for all the present voters
        foreach (var voter in EntityQueryEnumerator<ESVoterComponent, ActorComponent>())
        {
            SendVoteStartAnnouncement(ent, voter);
            _pvsOverride.AddSessionOverride(ent, voter.Comp2.PlayerSession);

            if (voter.Comp1.NotificationsEnabled)
                _uiSystem.TryOpenUi(voter.Owner, ESVoterUiKey.Key, voter.Owner);
        }

        OnVoteStart(ent);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnVoterPlayerAttached(Entity<ESVoterComponent> ent, ref PlayerAttachedEvent args)
    {
        foreach (var entry in EntityQueryEnumerator<MoffVoteEntryComponent>())
        {
            _pvsOverride.AddSessionOverride(entry, args.Player);
        }
    }

    [SubscribeLocalEvent]
    private void OnVoterPlayerDetached(Entity<ESVoterComponent> ent, ref PlayerDetachedEvent args)
    {
        foreach (var entry in EntityQueryEnumerator<MoffVoteEntryComponent>())
        {
            _pvsOverride.RemoveSessionOverride(entry, args.Player);
        }
    }

    public IEnumerable<Entity<MoffVoteEntryComponent>> EnumerateEntries()
    {
        foreach (var entity in EntityQueryEnumerator<MoffVoteEntryComponent>())
        {
            yield return entity;
        }
    }

    protected abstract void SendVoteStartAnnouncement(
        Entity<MoffVoteEntryComponent> ent,
        Entity<ESVoterComponent, ActorComponent> voter);

    protected abstract void OnVoteStart(Entity<MoffVoteEntryComponent> ent);
}
