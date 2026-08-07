using Content.Shared._Moffstation.Trigger.Components.Effects;
using Content.Shared.Temperature;
using Content.Shared.Trigger;

namespace Content.Shared._Moffstation.Trigger.Systems;

/// <summary>
/// This system implements the behavior of <see cref="TriggerOnTemperatureAboveComponent"/> and <see cref="TriggerOnTemperatureBelowComponent"/>.
/// </summary>
public sealed partial class TriggerOnTemperatureSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnTemperatureAboveComponent, OnTemperatureChangeEvent>(TempAboveOnTempChange);
        SubscribeLocalEvent<TriggerOnTemperatureBelowComponent, OnTemperatureChangeEvent>(TempBelowOnTempChange);
    }

    private void TempAboveOnTempChange(Entity<TriggerOnTemperatureAboveComponent> entity, ref OnTemperatureChangeEvent args)
    {
        if (args.CurrentTemperature > entity.Comp.Temperature &&
            args.LastTemperature <= entity.Comp.Temperature)
        {
            Trigger.Trigger(entity, key: entity.Comp.KeyOut);
        }
    }

    private void TempBelowOnTempChange(Entity<TriggerOnTemperatureBelowComponent> entity, ref OnTemperatureChangeEvent args)
    {
        if (args.CurrentTemperature < entity.Comp.Temperature &&
            args.LastTemperature >= entity.Comp.Temperature)
        {
            Trigger.Trigger(entity, key: entity.Comp.KeyOut);
        }
    }
}
