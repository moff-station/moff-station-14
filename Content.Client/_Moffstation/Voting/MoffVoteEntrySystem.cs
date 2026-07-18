using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Voting.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Voting;

/// <inheritdoc/>
public sealed partial class MoffVoteEntrySystem : ESSharedVoteSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // Moffstation - MoffVoteEntryComponent is a stateless marker, so it never raises
        // AfterAutoHandleStateEvent. Subscribe on the concrete entry types instead so open windows refresh
        // when a new vote/enroll syncs in, not just on removal.
        SubscribeLocalEvent<ESVoteComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<MoffEnrollEventComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<MoffVoteEntryComponent, ComponentRemove>(OnRemove);
    }

    private void OnAfterAutoHandleState(Entity<ESVoteComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOpenVoteWindows();
    }

    private void OnAfterAutoHandleState(Entity<MoffEnrollEventComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOpenVoteWindows();
    }

    private void OnRemove(Entity<MoffVoteEntryComponent> ent, ref ComponentRemove args)
    {
        RefreshOpenVoteWindows();
    }

    private void RefreshOpenVoteWindows()
    {
        if (!_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<ESVoterComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_userInterface.TryGetOpenUi((uid, ui), ESVoterUiKey.Key, out var bui))
                bui.Update();
        }
    }
}
