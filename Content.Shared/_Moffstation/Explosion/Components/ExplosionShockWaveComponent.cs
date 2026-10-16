namespace Content.Shared._Moffstation.Explosion.Components;

/// <summary>
///     A client-side screen-distortion ring expanding from this entity.
/// </summary>
[RegisterComponent]
public sealed partial class ExplosionShockWaveComponent : Component
{
    [DataField]
    public float FalloffPower = 40f;

    [DataField]
    public float Sharpness = 10.0f;

    [DataField]
    public float Width = 0.8f;

    /// <summary>
    ///     Real time the ring started expanding.
    /// </summary>
    [ViewVariables]
    public TimeSpan StartTime;
}
