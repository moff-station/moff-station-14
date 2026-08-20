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
