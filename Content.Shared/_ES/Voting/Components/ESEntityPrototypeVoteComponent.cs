using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

[Serializable, NetSerializable]
public sealed partial class ESEntityPrototypeVoteOption : ESVoteOption
{
    [DataField]
    public EntProtoId Entity;

    public override bool Equals(object? obj)
    {
        return obj is ESEntityPrototypeVoteOption other && Entity.Equals(other.Entity);
    }

    public override int GetHashCode()
    {
        return Entity.GetHashCode();
    }
}
