using Content.Shared._Moffstation.Damage.Components;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;

namespace Content.Server._Moffstation.Damage;

public sealed partial class DamageOnActivationSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedChatSystem _chat = default!;

    [SubscribeLocalEvent]
    private void OnActivate(Entity<DamageOnActivationComponent> ent, ref ActivateInWorldEvent args)
    {
        _damage.TryChangeDamage(ent.Owner, ent.Comp.Damage, ent.Comp.IgnoreResistances);
        _chat.DispatchGlobalAnnouncement("total damage :" + _damage.GetTotalDamage(ent.Owner));
    }
}
