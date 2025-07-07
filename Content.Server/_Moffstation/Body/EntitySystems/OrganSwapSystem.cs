using Content.Shared._Moffstation.Body.Components;
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
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OrganSwapComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedBodySystem)]);
    }

    /// <summary>
    /// Iterates through the entity's body, getting all the organ keys, then iterates through
    /// <see cref="Content.Shared._Moffstation.Body.Components.OrganSwapComponent.OrganSwaps"/>
    /// and replaces any matching keys with the new organ via the prototype provided.
    /// </summary>
    private void OnMapInit(Entity<OrganSwapComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp<OrganSwapComponent>(entity, out var comp))
            return;

        if (!TryComp<BodyComponent>(entity, out var body))
            return;

        // we want to manipulate the containers ourselves to not cause any side-effects with the organ system.
        foreach (var ( bodyPartUid, bodyPartComp) in _bodySystem.GetBodyChildren(entity, body))
        {
            foreach (var slotName in bodyPartComp.Organs.Keys)
            {
                foreach (var ( replaceSlotName, organProto) in comp.OrganSwaps)
                {
                    if (slotName != replaceSlotName)
                        continue;

                    if (!TryComp<ContainerManagerComponent>(bodyPartUid, out var containerManager))
                        continue;

                    TrySwapOrgan(_containerSystem.GetContainer(bodyPartUid,
                            SharedBodySystem.GetOrganContainerId(slotName),
                            containerManager),
                        organProto,
                        out _);

                    Dirty(bodyPartUid,bodyPartComp);
                }
            }
        }
    }

    /// <summary>
    /// Swaps organs in an entity's organ container.
    /// </summary>
    /// <param name="organContainer">The organ container to swap organs out off and create a new one in</param>
    /// <param name="organProto">The prototype to spawn in the organ container</param>
    /// <param name="organ">The newly created organ.</param>
    /// <returns>Whether we could successfully spawn the new organ or not.</returns>
    private bool TrySwapOrgan(BaseContainer organContainer, EntProtoId organProto, out EntityUid? organ)
    {
        // remove current organs and delete them
        // todo: handle issues with Diona causing the nymphs to spawn when the organs get deleted.
        _containerSystem.CleanContainer(organContainer);
        // spawn new organ and insert it
        return TrySpawnInContainer(organProto, organContainer.Owner, organContainer.ID, out organ);
    }
}
