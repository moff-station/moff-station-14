using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Damage.Components;

/// <summary>
/// Applies stamina damage proportional to the damage this projectile actually dealt.
/// </summary>
[RegisterComponent]
public sealed partial class ProjectileScaledStaminaDamageComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Blunt";

    [DataField]
    public float Factor = 4f;
}
