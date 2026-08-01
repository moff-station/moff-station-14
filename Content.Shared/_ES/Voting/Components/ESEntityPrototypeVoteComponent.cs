using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Voting.Components;

/// <summary>
/// Denotes sets of <see cref="ESVoteOption"/> that come from
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedVoteSystem))]
public sealed partial class ESEntityPrototypeVoteComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Options = new NoneSelector();
}
