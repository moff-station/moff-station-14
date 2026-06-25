using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Power.Components;

/// <summary>
/// This component gets added to a light during a light overload event causing it to explode.
/// See LightOverloadRuleSystem for more information.
/// </summary>
[RegisterComponent]
public sealed partial class LightExplodeTimerComponent : Component
{
    [DataField]
    public float ExplodeTimer = 0.0f;

    // The probability of sparks spawning at the light when it goes out.
    // This is set by the LightOverloadRuleSystem.
    [DataField]
    public float SparksProbability = 0.5f;

    // The type of sparks prototype to spawn on the lights when they explode.
    // This is set by the LightOverloadRuleSystem.
    [DataField]
    public EntProtoId SparksPrototype = "ESEffectSparks";
}
