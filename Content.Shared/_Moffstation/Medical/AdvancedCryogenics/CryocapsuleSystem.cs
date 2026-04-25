using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
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

    public bool TryGetBrain(Entity<CryocapsuleComponent> ent, [NotNullWhen(true)] out Entity<BrainComponent>? brain)
    {
        brain = null;

        if (!ent.Comp.BrainSlot.HasItem ||
            ent.Comp.BrainSlot.Item is not { } brainEnt ||
            ! TryComp<BrainComponent>(brainEnt, out var brainComp))
            return false;

        brain = (brainEnt, brainComp);
        return true;

    }

    private void OnCryocapsuleInit(Entity<CryocapsuleComponent> ent, ref ComponentInit init)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.BrainSlotId, ent.Comp.BrainSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.EyesSlotId, ent.Comp.EyesSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.LungSlotId, ent.Comp.LungSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.StomachSlotId, ent.Comp.StomachSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.HeartSlotId, ent.Comp.HeartSlot);
    }


    public CryoCapsuleEntry GenerateCryocapsuleEntry(Entity<CryocapsuleComponent> ent)
    {
        return new CryoCapsuleEntry(
            ent.Comp.BrainSlot.HasItem,
            ent.Comp.EyesSlot.HasItem,
            ent.Comp.LungSlot.HasItem,
            ent.Comp.HeartSlot.HasItem,
            ent.Comp.StomachSlot.HasItem
        );
    }
}
