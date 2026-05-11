using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._ES.Voting;

/// <inheritdoc/>
public sealed class ESVoteSystem : ESSharedVoteSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESVoteComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESVoteComponent, ComponentRemove>(OnRemove);
    }

    private void OnAfterAutoHandleState(Entity<ESVoteComponent> ent, ref AfterAutoHandleStateEvent args)
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

    private void OnRemove(Entity<ESVoteComponent> ent, ref ComponentRemove args)
    {
        var query = EntityQueryEnumerator<ESVoterComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_userInterface.TryGetOpenUi((uid, ui), ESVoterUiKey.Key, out var bui))
                bui.Update();
        }
    }
}
