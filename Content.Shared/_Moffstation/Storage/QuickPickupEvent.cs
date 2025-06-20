using Content.Shared._Moffstation.Storage.EntitySystems;
using Content.Shared.Item;

namespace Content.Shared._Moffstation.Storage;

/// <summary>
/// This event is raised on an entity with <see cref="QuickPickupComponent"/> when it attempts to pick up an item.
/// Storage-like systems which want to quick pickup into their storage SHOULD handle the event using
/// <see cref="QuickPickupSystem.TryDoQuickPickup"/>.
/// </summary>
/// <seealso cref="QuickPickupSystem"/>
/// <seealso cref="QuickPickupSystem.TryDoQuickPickup"/>
public sealed partial class QuickPickupEvent(
    Entity<QuickPickupComponent> entity,
    Entity<ItemComponent> pickedUp,
    EntityUid user
) : HandledEntityEventArgs
{
    public readonly Entity<QuickPickupComponent> QuickPickupEntity = entity;
    public readonly Entity<ItemComponent> PickedUp = pickedUp;
    public readonly EntityUid User = user;
}
