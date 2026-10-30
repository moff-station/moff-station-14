using Content.Shared.Damage;

namespace Content.Shared._Moffstation.Damage.Components;

/// <summary>
/// Damage the entity each time it is activated in world.
/// </summary>
[RegisterComponent]
public sealed partial class DamageOnActivationComponent : Component
{
    /// <summary>
    /// Damage taken by the entity when activated
    /// </summary>
    [DataField(required:true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Should the damage taken ignore the entity resistances ?
    /// </summary>
    [DataField("ignoreResistances")]
    public bool IgnoreResistances = true;
}
