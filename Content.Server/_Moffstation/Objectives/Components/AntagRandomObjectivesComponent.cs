using Content.Server.Antag.Components;

namespace Content.Server._Moffstation.Objectives.Components;

[RegisterComponent]
public sealed partial class AntagRandomObjectivesComponent : Component
{
    /// <summary>
    /// Each set of objectives to add.
    /// </summary>
    [DataField(required: true)]
    public List<AntagObjectiveSet> Sets = new();

    /// <summary>
    /// The amount of options to present to the player
    /// </summary>
    [DataField]
    public int MaxOptions = 6;

    /// <summary>
    /// The objective options presented to the player
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> ObjectiveOptions = new();
}
