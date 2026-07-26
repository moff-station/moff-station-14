using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._Moffstation.Objectives.Components;

/// <summary>
/// Component that can be given to objectives entities to have them add other objectives upon being chosen
/// </summary>
[RegisterComponent]
public sealed partial class MoffObjectivePackComponent : Component
{
    /// <summary>
    /// Entity table of the objectives that can be given
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives;

    /// <summary>
    /// Whether this objective should be kept
    /// </summary>
    [DataField]
    public bool KeepOriginal;
}
