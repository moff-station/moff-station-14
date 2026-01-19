using Content.Shared._Funkystation.EntityEffects.Effects.StatusEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Funkystation.EntityEffects.Systems;

public sealed partial class NitriumMovespeedModifierEntityEffectSystem
    : EntityEffectSystem<MovementSpeedModifierComponent, NitriumMovespeedModifier>
{
    [Dependency] private readonly MovementModStatusSystem _movementModStatus = default!;

    protected override void Effect(Entity<MovementSpeedModifierComponent> entity, ref EntityEffectEvent<NitriumMovespeedModifier> args)
    {
        var proto = args.Effect.EffectProto;
        var walkMod = args.Effect.WalkSpeedModifier;
        var sprintMod = args.Effect.SprintSpeedModifier;

        var duration = TimeSpan.FromSeconds(args.Effect.StatusLifetime * args.Scale);

        _movementModStatus.TryUpdateMovementSpeedModDuration(
            entity,
            proto,
            duration,
            sprintMod,
            walkMod);
    }
}
