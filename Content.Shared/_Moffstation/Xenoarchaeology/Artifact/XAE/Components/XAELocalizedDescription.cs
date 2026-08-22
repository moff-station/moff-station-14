namespace Content.Shared._Moffstation.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Replaces the default description with a localized string.
/// </summary>
[RegisterComponent]
public sealed partial class XAELocalizedDescriptionComponent : Component
{
    [DataField(required: true)]
    public LocId Description;
}
