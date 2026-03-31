namespace Content.Shared._Moffstation.Silicons.LawConfigurator;

/// <summary>
/// This is used for entity that react to contact with a LawReprogrammer component.
/// </summary>

[RegisterComponent]
public sealed partial class LawConfiguratorTargetComponent : Component
{
    /// <summary>
    /// Indicate if the entity is immune to reprogramming attempts.
    /// </summary>
    [DataField]
    public bool IsImmune = false;
}
