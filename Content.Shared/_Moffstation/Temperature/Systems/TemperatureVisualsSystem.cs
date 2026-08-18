using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Temperature.Components;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Shared._Moffstation.Temperature.Systems;

public sealed partial class TemperatureVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<TemperatureComponent> _temperatureQuery;

    [SubscribeLocalEvent]
    private void OnInit(Entity<TemperatureVisualsComponent> entity, ref ComponentInit args)
    {
        entity.Comp.VisualsSorted = new(entity.Comp.Visuals);

        // Initialize the visuals. This'll be wrong on the client until a state update from the server overwrites it.
        TemperatureComponent? temp = null;
        if (!_temperatureQuery.Resolve(entity.Owner, ref temp))
            return;

        _appearance.SetOrRemoveData(
            entity.Owner,
            TemperatureVisuals.Key,
            entity.Comp.VisualsSorted.GetContainingRange(temp.Temperature).below?.Value
        );
    }

    [SubscribeLocalEvent]
    private void OnTemperatureChange(Entity<TemperatureVisualsComponent> entity, ref TemperatureChangedEvent args)
    {
        var (previousLower, previousUpper) = entity.Comp.VisualsSorted.GetContainingRange(args.LastTemperature);
        var (lower, upper) = entity.Comp.VisualsSorted.GetContainingRange(args.CurrentTemperature);

        if (previousLower?.Key == lower?.Key &&
            previousUpper?.Key == upper?.Key)
        {
            // No change, avoid dirtying appearance data.
            return;
        }

        _appearance.SetOrRemoveData(entity.Owner, TemperatureVisuals.Key, lower?.Value);
    }
}
