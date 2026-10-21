using Content.Shared.DisplacementMap;

// ReSharper disable once CheckNamespace // Moff addition to upstream class
namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    /// Sets the base displacements for this entity's inventory component.
    public void SetDisplacements(
        Entity<InventoryComponent> entity,
        Dictionary<string, DisplacementData> newDisplacements
    )
    {
        entity.Comp.Displacements = newDisplacements;
        Dirty(entity);
    }

    /// Sets <see cref="InventoryComponent.SpeciesId"/>.
    public void SetSpeciesId(Entity<InventoryComponent> entity, string speciesId)
    {
        entity.Comp.SpeciesId = speciesId;
        Dirty(entity);
    }
}
