using Content.Shared.Damage;

namespace Content.Shared._Moffstation.Projectiles;

/// <summary>
/// Raised on a projectile after it has dealt its damage, opposed to ProjectileHitEvent which is raised before.
/// </summary>
[ByRefEvent]
public readonly record struct ProjectileDamageDealtEvent(DamageSpecifier Damage, EntityUid Target, EntityUid? Shooter = null);
