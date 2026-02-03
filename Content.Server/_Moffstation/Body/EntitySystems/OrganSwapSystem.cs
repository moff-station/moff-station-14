using Content.Shared._Moffstation.Body.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Body.EntitySystems;

/// <summary>
/// Swaps the organs of an entity on MapInit.
/// </summary>
public sealed partial class OrganSwapSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OrganSwapComponent, MapInitEvent>(OnMapInit, after: [typeof(BodySystem)]);
    }

    private void OnMapInit(Entity<OrganSwapComponent> entity, ref MapInitEvent args)
    {
        // Centronias rewrote this without testing on 2026-01-18 due to breaking API changes from https://github.com/space-wizards/space-station-14/pull/42419/
        // so this may not work anymore, idk.
        if (!TryComp<BodyComponent>(entity, out var body))
            return;

        var bodyPartContainer = _containerSystem.GetContainer(entity, BodyComponent.ContainerID);
        foreach (var bodyPart in bodyPartContainer.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(bodyPart, out var organComp) ||
                organComp.Category is not { } category ||
                !entity.Comp.OrganSwaps.TryGetValue(category, out var swap))
                continue;

            _containerSystem.Remove(bodyPart, bodyPartContainer, force: true);
            QueueDel(bodyPart);
            TrySpawnInContainer(swap, entity, BodyComponent.ContainerID, out _);
        }
    }
}
