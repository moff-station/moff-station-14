using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._Moffstation.StationEvents.Components;

/// <summary>
/// Our beloved custom event scheduler. Well... Not actually sure if its beloved yet, hopefully it will be!
/// It works in a similar way to the normal event scheduler, except it gives you more tools to change timing midround.
/// </summary>
[RegisterComponent]
public sealed partial class MoffStationEventSchedulerComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, MoffEventSchedulerState> States = new();

    [DataField(required: true)]
    public string InitialState = string.Empty;

    [DataField]
    public MinMax InitialDelaySeconds = new(200, 320);

    [DataField(required: true)]
    public EntityTableSelector ScheduledGameRules = default!;

    [ViewVariables]
    public string? CurrentStateId;

    [ViewVariables]
    public MoffEventSchedulerState? CurrentState => CurrentStateId is { } cs && States.TryGetValue(cs, out var state) ? state : null;

    [ViewVariables]
    public TimeSpan NextEventTime;

    [ViewVariables]
    public TimeSpan? NextStateTime;
}

[DataDefinition]
public sealed partial class MoffEventSchedulerState
{
    /// <summary>
    /// The minimum and maximum time (in seconds) that this scheduler state will last before moving onto a new state
    /// </summary>
    [DataField]
    public MinMax? Duration;

    /// <summary>
    /// The minimum and max time (in seconds) between events for this scheduler state
    /// </summary>
    [DataField]
    public MinMax? MinMaxEventTiming;

    /// <summary>
    /// The IDs of possible next states for this state, paired with their respective weights.
    /// </summary>
    [DataField]
    public Dictionary<string, float> NextStates = new();

    /// <summary>
    /// Whether an event should be ran when this state ends.
    /// </summary>
    [DataField]
    public bool EventOnEnd;
}
