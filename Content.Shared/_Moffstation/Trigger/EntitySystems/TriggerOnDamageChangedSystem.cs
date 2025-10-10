using Content.Shared._Moffstation.Trigger.Components.Triggers;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Systems;

namespace Content.Shared._Moffstation.Trigger.EntitySystems;

/// <summary>
/// This system implements <see cref="TriggerOnDamageChangedComponent"/> and its relayed siblings.
/// </summary>
public sealed partial class TriggerOnDamageChangedSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerOnDamageChangedComponent, DamageChangedEvent>(OnDamageChangedGeneric);
        SubscribeLocalEvent<TriggerOnEquipeeDamageChangedComponent, InventoryRelayedEvent<DamageChangedEvent>>(
            OnDamageChangedGeneric);
        SubscribeLocalEvent<TriggerOnHolderDamageChangedComponent, HeldRelayedEvent<DamageChangedEvent>>(
            OnDamageChangedGeneric);
    }

    private void OnDamageChangedGeneric<TComp, TArgs>(Entity<TComp> entity, ref TArgs args)
        where TComp : BaseTriggerOnXComponent
    {
        _trigger.Trigger(entity, null, entity.Comp.KeyOut);
    }
}
