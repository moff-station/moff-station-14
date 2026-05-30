using Content.Shared._ES.Voting.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._ES.StationEvents.Scheduler.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESEventVoteSchedulerSystem))]
public sealed partial class ESEventVoteSchedulerComponent : Component
{
    /// <summary>
    /// Table of events to use.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector EventTable = new NoneSelector();

    /// <summary>
    /// Prototype for the vote entity that is spawned
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ESVoteComponent> VotePrototype;

    /// <summary>
    /// The number of options per vote
    /// </summary>
    [DataField]
    public int VoteOptions = 4;

    /// <summary>
    /// Delayed added on top of the event delay for the first event ran.
    /// </summary>
    [DataField]
    public TimeSpan BaseFirstEventDelay = TimeSpan.FromMinutes(2.5f);

    /// <summary>
    /// Minimum amount of time between votes being created.
    /// </summary>
    [DataField]
    public TimeSpan MinEventDelay = TimeSpan.FromMinutes(3f);

    /// <summary>
    /// Maximum amount of time between votes being created
    /// </summary>
    [DataField]
    public TimeSpan MaxEventDelay = TimeSpan.FromMinutes(10f);

    /// <summary>
    /// Time at which the next vote will be started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEventTime;
}
