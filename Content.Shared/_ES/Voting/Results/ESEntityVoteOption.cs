using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Results;

[Serializable, NetSerializable]
public sealed partial class ESEntityVoteOption : ESVoteOption
{
    // Weak Entity Ref Terrorist
    [DataField]
    public NetEntity Entity;

    public override bool Equals(object? obj)
    {
        return obj is ESEntityVoteOption other && Entity.Equals(other.Entity);
    }

    public override int GetHashCode()
    {
        return Entity.GetHashCode();
    }
}
