using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryocapsuleSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryocapsuleComponent, ComponentInit>(OnCryocapsuleInit);
    }

    private void OnCryocapsuleInit(Entity<CryocapsuleComponent> ent, ref ComponentInit init)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.BrainSlotId, ent.Comp.BrainSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.LungSlotId, ent.Comp.LungSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.StomachSlotId, ent.Comp.StomachSlot);
    }
}
