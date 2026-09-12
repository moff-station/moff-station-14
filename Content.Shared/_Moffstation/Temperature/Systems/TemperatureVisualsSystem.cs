using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Temperature.Components;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;

namespace Content.Shared._Moffstation.Temperature.Systems;

public sealed partial class TemperatureVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<TemperatureVisualsComponent> entity, ref MapInitEvent args)
    {
        var temp = EntityManager.EnsureComp<TemperatureComponent>(entity);
        _appearance.SetOrRemoveData(
            entity.Owner,
            TemperatureVisuals.Key,
            entity.Comp.Visuals.GetContainingRange(temp.Comp.Temperature).below?.Value
        );
    }

    [SubscribeLocalEvent]
    private void OnTemperatureChange(Entity<TemperatureVisualsComponent> entity, ref TemperatureChangedEvent args)
    {
        var (previousLower, previousUpper) = entity.Comp.Visuals.GetContainingRange(args.LastTemperature);
        var (lower, upper) = entity.Comp.Visuals.GetContainingRange(args.CurrentTemperature);

        if (previousLower?.Key == lower?.Key &&
            previousUpper?.Key == upper?.Key)
        {
            // No change, avoid dirtying appearance data.
            return;
        }

        _appearance.SetOrRemoveData(entity.Owner, TemperatureVisuals.Key, lower?.Value);
    }
}
