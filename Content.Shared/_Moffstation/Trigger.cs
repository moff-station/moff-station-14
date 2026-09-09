using Content.Shared.EntityEffects;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Moffstation;

public sealed partial class TriggerEntityEffectSystem : EntityEffectSystem<MetaDataComponent, TriggerEffect>
{
    [Dependency] private TriggerSystem _trigger = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<TriggerEffect> args)
    {
        _trigger.Trigger(entity, args.User, args.Effect.KeyOut);
    }
}

/// This effect simply triggers a trigger, as by <see cref="TriggerSystem"/>.
public sealed partial class TriggerEffect : EntityEffectBase<TriggerEffect>
{
    [DataField]
    public string? KeyOut = TriggerSystem.DefaultTriggerKey;
}
