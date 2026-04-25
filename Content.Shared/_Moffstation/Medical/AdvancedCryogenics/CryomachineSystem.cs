using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles...
/// </summary>
public class SharedCryomachineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly IGameTiming _time = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem _ui = default!;


    /// <inheritdoc/>

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryomachineComponent, ComponentInit>(OnCryomachineInit);
        SubscribeLocalEvent<CryomachineComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<CryomachineComponent, EntRemovedFromContainerMessage>(OnRemoved);

        Subs.BuiEvents<CryomachineComponent>(CryomachineUiKey.Key, subs =>
            {
                subs.Event<CryomachineSimpleUiMessage>(OnSimpleUiMessage);
            });
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

    private void OnSimpleUiMessage(Entity<CryomachineComponent> ent, ref CryomachineSimpleUiMessage args)
    {
        switch (args.Type)
        {
            case CryomachineSimpleUiMessage.MessageType.JumpstartBrain :
                _audio.PlayPredicted(ent.Comp.ShockSound, ent.Owner, ent.Owner, AudioParams.Default);
                break;
            case CryomachineSimpleUiMessage.MessageType.DetachCapsule :
                // todo : detach the capsule from the container.
                _audio.PlayPredicted(ent.Comp.DetachSound, ent.Owner, ent.Owner, AudioParams.Default);
                break;
            case CryomachineSimpleUiMessage.MessageType.EjectBeaker:
                break;
        }
    }
}

[Serializable, NetSerializable]
public enum CryomachineVisuals : byte
{
    Filled,
}
