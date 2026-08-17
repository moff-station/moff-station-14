using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._Moffstation.StationEvents.Components;

/// <summary>
/// An event scheduler which moves between named <see cref="MoffEventSchedulerState"/>s, each with its own event cadence.
/// A single never-expiring state behaves like <c>BasicStationEventScheduler</c>, while a lull/wave pair produces bursts.
/// </summary>
[RegisterComponent]
public sealed partial class MoffStationEventSchedulerComponent : Component
{
    /// <summary>
    /// Every state this scheduler can be in, keyed by the name used in <see cref="InitialState"/> and
    /// <see cref="MoffEventSchedulerState.NextStates"/>.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, MoffEventSchedulerState> States = new();

    /// <summary>
    /// The state entered when the rule starts.
    /// </summary>
    [DataField(required: true)]
    public string InitialState = string.Empty;

    /// <summary>
    /// Seconds until the first event, so schedulers don't all fire the moment the round begins.
    /// </summary>
    [DataField]
    public MinMax InitialDelay = new(200, 320);

    /// <summary>
    /// The gamerules this scheduler picks from, unless the current state overrides it.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;

    /// <summary>
    /// The state currently being run. Null if the scheduler failed to start.
    /// </summary>
    [ViewVariables]
    public string? CurrentState;

    /// <summary>
    /// Seconds until the next event fires.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextEventTime;

    /// <summary>
    /// Seconds until <see cref="CurrentState"/> expires. Null while in a state which never expires.
    /// </summary>
    [ViewVariables]
    public TimeSpan? NextStateTime;
}

/// <summary>
/// One state of a <see cref="MoffStationEventSchedulerComponent"/>.
/// </summary>
[DataDefinition]
public sealed partial class MoffEventSchedulerState
{
    /// <summary>
    /// How long this state lasts, in seconds. Null means it never expires.
    /// </summary>
    [DataField]
    public MinMax? Duration;

    /// <summary>
    /// Seconds between events while in this state. Null means no events fire.
    /// </summary>
    [DataField]
    public MinMax? EventTiming;

    /// <summary>
    /// Weighted states this one can move to once <see cref="Duration"/> elapses.
    /// </summary>
    [DataField]
    public Dictionary<string, float> NextStates = new();

    /// <summary>
    /// Fires an event the instant this state is entered.
    /// </summary>
    [DataField]
    public bool EventOnEnter;
}
