using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Harmony.GameTicking.Rules.Components;

/// <summary>
/// Game rule for blood brothers. Handles conversion.
/// </summary>
[RegisterComponent, Access(typeof(BloodBrotherRuleSystem)), AutoGenerateComponentPause]
public sealed partial class BloodBrotherRuleComponent : Component
{
    [DataField]
    public TimeSpan ConvertibleUpdateInterval = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextConvertibleUpdate;
}
