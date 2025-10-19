using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Voting.Components;

/// <summary>
/// An entity that holds data regarding a vote for <see cref="ESVoterComponent"/> entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(ESSharedVoteSystem))]
[EntityCategory("ESVotes")]
public sealed partial class ESVoteComponent : Component
{
    /// <summary>
    /// When the vote will end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// Total duration of vote.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Dictionary relating the different options for a vote to the people who have voted for it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ESVoteOption, HashSet<NetEntity>> Votes = new();

    /// <summary>
    /// The different options that can be voted on
    /// </summary>
    [ViewVariables]
    public List<ESVoteOption> VoteOptions => Votes.Keys.ToList();

    /// <summary>
    /// String that is used in chat when the vote completes.
    /// This is a line of text appended directly by the vote's <see cref="ESVoteOption.DisplayString"/>
    /// </summary>
    [DataField]
    public LocId QueryString = "es-voter-chat-announce-query-default";

    /// <summary>
    /// How the result is selected
    /// </summary>
    [DataField, AutoNetworkedField]
    public ResultStrategy Strategy = ResultStrategy.HighestValue;

    /// <summary>
    /// If false, voters will not be able to see how many votes they have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowCount = true;
}

/// <summary>
/// Methods of determining the result for <see cref="ESVoteComponent"/>
/// </summary>
public enum ResultStrategy : byte
{
    HighestValue,
    WeightedPick,
}

/// <summary>
/// Event raised on a vote entity to retrieve the options available for voting.
/// This is run once when the vote begins.
/// </summary>
[ByRefEvent]
public record struct ESGetVoteOptionsEvent()
{
    /// <summary>
    /// The options for voting.
    /// </summary>
    public readonly List<ESVoteOption> Options = new();
}

/// <summary>
/// Event raised on a vote entity and broadcast when the vote concludes.
/// </summary>
/// <param name="Vote">The vote that was completed.</param>
/// <param name="Result">The result of the vote</param>
[ByRefEvent]
public readonly record struct ESVoteCompletedEvent(Entity<ESVoteComponent> Vote, ESVoteOption Result);
