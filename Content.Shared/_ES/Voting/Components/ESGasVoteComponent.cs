using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Voting.Components;

/// <summary>
/// A vote that uses a set of gases as options
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedVoteSystem))]
public sealed partial class ESGasVoteComponent : Component
{
    /// <summary>
    /// The gases that will be selected from
    /// </summary>
    [DataField]
    public List<Gas> Gases = new();

    /// <summary>
    /// Number of options
    /// </summary>
    [DataField]
    public int Count = 4;
}
