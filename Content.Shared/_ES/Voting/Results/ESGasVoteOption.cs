using Content.Shared.Atmos;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Results;

[Serializable, NetSerializable]
public sealed partial class ESGasVoteOption : ESVoteOption
{
    [DataField]
    public Gas Gas;

    public override bool Equals(object? obj)
    {
        return obj is ESGasVoteOption other && Gas.Equals(other.Gas);
    }

    public override int GetHashCode()
    {
        return Gas.GetHashCode();
    }
}
