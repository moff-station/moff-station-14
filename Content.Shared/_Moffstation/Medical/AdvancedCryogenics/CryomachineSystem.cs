using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryomachineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryomachineComponent, ComponentInit>(OnCryomachineInit);
        SubscribeLocalEvent<CryomachineComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<CryomachineComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    // TODO : make the sound play only when the machine is full.

    private void OnCryomachineInit(Entity<CryomachineComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.CapsuleSlotId, ent.Comp.CapsuleSlot);
    }

    private void OnInserted(Entity<CryomachineComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapsuleSlotId)
            return;

        _appearance.SetData(ent.Owner, CryomachineVisuals.Filled, true);
    }

    private void OnRemoved(Entity<CryomachineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapsuleSlotId)
            return;

        _appearance.SetData(ent.Owner, CryomachineVisuals.Filled, false);
    }
}

[Serializable, NetSerializable]
public enum CryomachineVisuals : byte
{
    Filled,
}
