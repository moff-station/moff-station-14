using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryomachineSystem : EntitySystem
{
    /// <inheritdoc/>

    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryomachineComponent, ComponentInit>(OnCryocapsuleInit);
    }

    private void OnCryocapsuleInit(Entity<CryomachineComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.CapsuleSlotId, ent.Comp.CapsuleSlot);
    }
}
