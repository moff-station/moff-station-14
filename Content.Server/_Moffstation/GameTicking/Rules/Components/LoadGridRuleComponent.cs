using Robust.Shared.Utility;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class LoadGridRuleComponent : Component
{
    /// <summary>
    /// Path to the grid to be loaded
    /// </summary>
    [DataField]
    public ResPath GridPath = new();

    [DataField]
    public float MinimumDistance = 100f;

    [DataField]
    public float MaximumDistance = 1000f;

    /// <summary>
    /// Radius in which to check for collisions for the spawned grid
    /// </summary>
    [DataField]
    public float SafetyZoneRadius = 16f;

    /// <summary>
    /// Max amount of attempts to find an unobstructed location for the grid
    /// </summary>
    [DataField]
    public int MaxAttempts = 100;
}
