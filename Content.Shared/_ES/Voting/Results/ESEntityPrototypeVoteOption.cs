using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Results;

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

    public ESEntityPrototypeVoteOption(EntityPrototype prototype)
    {
        DisplayString = prototype.Name;
        Entity = prototype;
    }
}
