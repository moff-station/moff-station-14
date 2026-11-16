namespace Content.Shared._Moffstation.Sight.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SightTargetComponent : Component
{
    [DataField(required: true)]
    public string Key = "";
}
