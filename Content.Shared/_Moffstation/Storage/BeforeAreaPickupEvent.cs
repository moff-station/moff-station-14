using Content.Shared._Moffstation.Storage.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Storage;

namespace Content.Shared._Moffstation.Storage;

/// <summary>
/// This event is raised on an entity with <see cref="AreaPickupComponent"/> to allow storage-like systems to inform
/// the area pickup logic which entities can actually be picked up by the entity. A storage-like system which wants to
/// implement area pickup into its storage MUST also handle <see cref="AreaPickupDoAfterEvent"/> and SHOULD use
/// <see cref="AreaPickupSystem.DoBeforeAreaPickup"/> to handle this event.
/// </summary>
/// <seealso cref="AreaPickupDoAfterEvent"/>
/// <seealso cref="AreaPickupSystem"/>
/// <seealso cref="AreaPickupSystem.DoBeforeAreaPickup"/>
public sealed partial class BeforeAreaPickupEvent(
    IReadOnlyList<Entity<ItemComponent>> pickupCandidates,
    int maxPickups
) : HandledEntityEventArgs
{
    public readonly IReadOnlyList<Entity<ItemComponent>> PickupCandidates = pickupCandidates;
    public readonly List<Entity<ItemComponent>> EntitiesToPickUp = [];
    public readonly int MaxPickups = maxPickups;
}
