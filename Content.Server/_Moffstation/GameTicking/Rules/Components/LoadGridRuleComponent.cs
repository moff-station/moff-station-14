using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(PiratesRuleSystem))]

public sealed partial class LoadGridRuleComponent : Component
{
    /// <summary>
    /// Path to the grid to be loaded
    /// </summary>
    [DataField]
    public ResPath GridPath;

    [DataField]
    public float MinimumDistance = 100f;

    [DataField]
    public float MaximumDistance = 1000f;
}
