using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Content.Shared._Moffstation.Extensions;
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
        SubscribeLocalEvent<ESVoteComponent, AfterAutoHandleStateEvent>(RefreshOpenVoteWindows);
        SubscribeLocalEvent<MoffEnrollEventComponent, AfterAutoHandleStateEvent>(RefreshOpenVoteWindows);
        SubscribeLocalEvent<MoffVoteEntryComponent, ComponentRemove>(RefreshOpenVoteWindows);
    }

    private void RefreshOpenVoteWindows<TComp, TArgs>(Entity<TComp> ent, ref TArgs args) where TComp : Component
    {
        if (!_timing.ApplyingState)
            return;

        foreach (var entity in EntityQueryEnumerator<ESVoterComponent, UserInterfaceComponent>().AsEnumerable())
        {
            if (_userInterface.TryGetOpenUi((entity, entity), ESVoterUiKey.Key, out var bui))
                bui.Update();
        }
    }
}
