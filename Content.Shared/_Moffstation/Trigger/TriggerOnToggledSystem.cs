using Content.Shared._Moffstation.Trigger.Components.Effects;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Trigger;

namespace Content.Shared._Moffstation.Trigger;

/// <summary>
/// Implements the behavior of <see cref="TriggerOnToggleOnComponent"/> and <see cref="TriggerOnToggleOffComponent"/>.
/// </summary>
public sealed partial class TriggerOnToggledSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnToggleOnComponent, ItemToggledEvent>(ToggleOn);
        SubscribeLocalEvent<TriggerOnToggleOffComponent, ItemToggledEvent>(ToggleOff);
    }

    private void ToggleOn(Entity<TriggerOnToggleOnComponent> entity, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            return;

        Trigger.Trigger(entity, args.User, entity.Comp.KeyOut, args.Predicted);
    }

    private void ToggleOff(Entity<TriggerOnToggleOffComponent> entity, ref ItemToggledEvent args)
    {
        if (args.Activated)
            return;

        Trigger.Trigger(entity, args.User, entity.Comp.KeyOut, args.Predicted);
    }
}
