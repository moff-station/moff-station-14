using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Voting.Components;

/// <summary>
/// This is used for an entity which spawns multiple child <see cref="ESVoteComponent"/> entities.
/// These votes are run in tandem and, when completed, raise an event to handle combined behavior.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedVoteSystem))]
public sealed partial class ESSynchronizedVoteManagerComponent : Component
{
    [ViewVariables]
    public bool Completed => !Results.Any(r => r is null);

    /// <summary>
    /// The vote prototypes that will be spawned
    /// </summary>
    [DataField]
    public List<EntProtoId<ESVoteComponent>> Votes = new();

    /// <summary>
    /// Currently active votes that are being run
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> VoteEntities = new();

    /// <summary>
    /// The results for all the completed votes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ESVoteOption?> Results = new();
}

/// <summary>
/// Event raised on a synchronized vote entity when all of its votes are completed.
/// </summary>
/// <param name="Results"></param>
[ByRefEvent]
public readonly record struct ESSynchronizedVotesCompletedEvent(List<ESVoteOption> Results)
{
    public bool TryGetResult<T>(int idx, [NotNullWhen(true)] out T? result) where T : ESVoteOption
    {
        result = null;

        if (!Results.TryGetValue(idx, out var value))
            return false;

        if (value is not T res)
            return false;

        result = res;
        return true;
    }
}

/// <summary>
/// Event raised after <see cref="ESSynchronizedVotesCompletedEvent"/>
/// </summary>
[ByRefEvent]
public readonly record struct ESSynchronizedVotesPostCompletedEvent;
