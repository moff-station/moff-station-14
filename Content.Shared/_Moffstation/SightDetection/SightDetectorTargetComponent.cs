namespace Content.Shared._Moffstation.SightDetection;

/// <summary>
///     Make the entity able to be detected by a  <see cref="SightDetectorComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class SightDetectorTargetComponent : Component
{
    [DataField(required: true)]
    public string Key = default!;
}
