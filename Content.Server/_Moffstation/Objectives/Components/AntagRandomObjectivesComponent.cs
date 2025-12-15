using Content.Server.Antag.Components;
using Robust.Shared.Player;

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
    /// Kept for compatibility with the old version
    /// </summary>
    [DataField]
    public float MaxDifficulty = float.MaxValue;
}
