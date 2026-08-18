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
    [DataField(required: true)]
    public Dictionary<string, MoffEventSchedulerState> States = new();

    [DataField(required: true)]
    public string InitialState = string.Empty;

    [DataField]
    public MinMax InitialDelay = new(200, 320);

    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;

    [ViewVariables]
    public string? CurrentState;

    [ViewVariables]
    public TimeSpan NextEventTime;

    [ViewVariables]
    public TimeSpan? NextStateTime;
}

[DataDefinition]
public sealed partial class MoffEventSchedulerState
{
    [DataField]
    public MinMax? Duration;

    [DataField]
    public MinMax? MinMaxEventTiming;

    /// <summary>
    /// The next state to move to, this can be multiple states weighted against eachother.
    /// </summary>
    [DataField]
    public Dictionary<string, float> NextStates = new();

    [DataField]
    public bool EventOnEnd;
}
