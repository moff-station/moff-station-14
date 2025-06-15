using Content.Server._Moffstation.Vampire.Abilities.Components;
using Content.Shared._Moffstation.Vampire.Events;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._Moffstation.Vampire.Abilities.EntitySystems;

public sealed class AbilityRejuvenateSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem  _stamina = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbilityRejuvenateComponent, VampireEventRejuvenateAbility>(OnRejuvenate);
        SubscribeLocalEvent<AbilityRejuvenateComponent, MapInitEvent>(OnMapInit);
    }

    public void OnMapInit(EntityUid uid, AbilityRejuvenateComponent? comp, MapInitEvent args)
    {
        if (!Resolve(uid, ref comp))
            return;
        _action.AddAction(uid, ref comp.Action, comp.ActionProto, uid);
    }

    private void OnRejuvenate(EntityUid uid, AbilityRejuvenateComponent? comp, VampireEventRejuvenateAbility args)
    {
        if (args.Handled)
            return;

        if (!Resolve(uid, ref comp))
            return;

        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        _stamina.TakeStaminaDamage(uid, -Math.Abs(comp.StamHealing));

        args.Handled = true;
    }
}
