namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// Used to handle organisms placed inside a CLS
/// </summary>

[RegisterComponent]
public sealed partial class InsideCryomachineComponent : Component
{
    /// <summary>
    /// Cryomachine currently containing this entity
    /// </summary>
    [DataField]
    public EntityUid? Cryomachine;
}

