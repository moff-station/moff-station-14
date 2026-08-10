using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._Moffstation.Objectives.Components;


/// <summary>
/// When an objective with component is chosen, it will spawn and assign an EntityTable of objectives to the person's mind
/// </summary>
[RegisterComponent]
public sealed partial class MoffObjectivePackComponent : Component
{
    /// <summary>
    /// Entity table of the objectives that can be given
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives = default!;

    /// <summary>
    /// Whether this objective should be kept
    /// </summary>
    [DataField]
    public bool KeepOriginalObjective;
}
