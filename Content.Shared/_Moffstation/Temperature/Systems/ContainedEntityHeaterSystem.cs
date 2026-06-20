using Content.Shared._Moffstation.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Temperature.Systems;

/// <summary>
/// Implements the behavior of <see cref="ContainedEntityHeaterComponent"/>.
/// </summary>
public sealed partial class ContainedEntityHeaterSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainedEntityHeaterComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<ContainedEntityHeaterComponent> entity, ref ComponentInit args)
    {
        // Prediction means this sometimes if null. Very cool.
        if (entity.Comp.ContainerId is { } compContainerId)
        {
            entity.Comp.Container = _container.EnsureContainer<BaseContainer>(entity, compContainerId);
        }
    }

    public override void Update(float frameTime)
    {
        foreach (var comp in EntityQuery<ContainedEntityHeaterComponent>())
        {
            if (comp.Container is not { } container)
                continue;

            var contents = container.ContainedEntities;
            var powerPer = comp.Power / frameTime / contents.Count;
            foreach (var contained in contents)
            {
                _temperature.ChangeHeat(contained, powerPer, comp.IgnoreHeatResistance);
            }
        }
    }
}
