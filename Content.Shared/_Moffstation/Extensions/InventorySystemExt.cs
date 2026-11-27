using Content.Shared.Inventory;

namespace Content.Shared._Moffstation.Extensions;

public static class InventorySystemExt
{
    extension(InventorySystem inventorySys)
    {
        /// Returns all of the entities equipped to <paramref name="entity"/>'s <see cref="InventoryComponent">inventory</see>
        /// in the requested <paramref name="slots"/>. If <paramref name="entity"/> does not have an <see cref="InventoryComponent"/>,
        /// returns an empty enumerable.
        public IEnumerable<(EntityUid, SlotDefinition)> GetAllEquippedEntities(
            Entity<InventoryComponent?> entity,
            SlotFlags slots = SlotFlags.All
        )
        {
            var enumerator = inventorySys.GetSlotEnumerator(entity, slots);
            while (enumerator.NextItem(out var equipped, out var slot))
            {
                yield return (equipped, slot);
            }
        }

        /// <see cref="InventorySystemExt.GetAllEquippedEntities"/>, but returns only the equipped entities which have
        /// <typeparamref name="T"/>.
        public IEnumerable<(Entity<T>, SlotDefinition)> GetAllEquippedEntitiesWithComp<T>(
            Entity<InventoryComponent?> entity,
            EntityQuery<T> query,
            SlotFlags slots = SlotFlags.All
        ) where T : Component
        {
            foreach (var (equipped, slot) in inventorySys.GetAllEquippedEntities(entity, slots))
            {
                if (query.ResolveOrNull(equipped, logMissing: false) is { } withComp)
                {
                    yield return (withComp, slot);
                }
            }
        }
    }
}
