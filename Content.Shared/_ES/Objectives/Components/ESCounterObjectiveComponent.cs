using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Components;

/// <summary>
/// Specific tracker component for <see cref="ESObjectiveComponent"/> that tracks accumulated values and networks them.
/// Essentially a giant helper system that
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedObjectiveSystem), Other = AccessPermissions.None)]
public sealed partial class ESCounterObjectiveComponent : Component
{
    /// <summary>
    /// Continuously incremented value used to track progress.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Counter;

    /// <summary>
    /// Target value <see cref="Counter"/> must equal to qualify the objective as complete.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Target = 1;

    /// <summary>
    /// If set, will randomize the value of <see cref="Target"/> on the interval of [<see cref="Target"/>, <see cref="MaxTarget"/>] in increments of <see cref="TargetIncrement"/>
    /// </summary>
    [DataField]
    public float? MaxTarget;

    /// <summary>
    /// Size of "steps" between minimum and maximum target values.
    /// Only used if <see cref="MaxTarget"/> is not null.
    /// </summary>
    [DataField]
    public float TargetIncrement = 1;

    /// <summary>
    /// Optional title locale id, passed "count" with <see cref="Target"/>.
    /// </summary>
    [DataField]
    public LocId? Title;

    /// <summary>
    /// Optional description locale id, passed "count" with <see cref="Target"/>.
    /// </summary>
    [DataField]
    public LocId? Description;
}
