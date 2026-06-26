using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

/// <summary>
/// This component is for round event rules which selects an APC and explodes lights in a radius around it.
/// <seealso cref="LightOverloadRuleSystem"/>
/// </summary>
[RegisterComponent]
public sealed partial class LightOverloadRuleComponent : Component
{
    // The radius around the APC to search for lights to explode.
    // This is needed because there isn't an easy way to determine which APC a light is connected to.
    [DataField]
    public float Radius = 8.0f;

    // The type of sparks prototype to spawn on the lights when they explode.
    [DataField]
    public EntProtoId SparksPrototype = "ESEffectSparks";

    // The probability of sparks spawning at the light when it goes out.
    [DataField]
    public float SparksProbability = 0.5f;

    // The probability that a light within the radius blows up.
    [DataField]
    public float LightOverloadProbability = 0.8f;

    // The maximum delay before a light explodes.
    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromSeconds(5.0);

    // The probability that a light becomes a blinking light.
    [DataField]
    public float LightBlinkingProbability = 0.2f;

    // The amount of time a light will remain blinking.
    [DataField]
    public TimeSpan BlinkTime =  TimeSpan.FromSeconds(30.0);

    // The announcement to make when this rule triggers.
    [DataField]
    public string Announcement = "light-overload-announcement";

    // The color to use when making the announcement
    [DataField]
    public Color AnnouncementColor = Color.Yellow;
}
