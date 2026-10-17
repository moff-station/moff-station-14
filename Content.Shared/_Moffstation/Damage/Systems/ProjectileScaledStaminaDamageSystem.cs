using Content.Shared._Moffstation.Damage.Components;
using Content.Shared._Moffstation.Projectiles;
using Content.Shared.Damage.Systems;

namespace Content.Shared._Moffstation.Damage.Systems;

public sealed partial class ProjectileScaledStaminaDamageSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    [SubscribeLocalEvent]
    private void OnProjectileDamageDealt(Entity<ProjectileScaledStaminaDamageComponent> ent, ref ProjectileDamageDealtEvent args)
    {
        if (!args.Damage.DamageDict.TryGetValue(ent.Comp.DamageType, out var dealt) || dealt <= 0)
            return;

        _stamina.TakeStaminaDamage(
            args.Target,
            dealt.Float() * ent.Comp.Factor,
            source: ent,
            sound: null);
    }
}
