using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryocapsuleSystem : EntitySystem
{
    /// <inheritdoc/>

    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryocapsuleComponent, ComponentInit>(OnCryocapsuleInit);
    }

    private void OnCryocapsuleInit(Entity<CryocapsuleComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.CasketSlotId, ent.Comp.CasketSlot);
    }
}
