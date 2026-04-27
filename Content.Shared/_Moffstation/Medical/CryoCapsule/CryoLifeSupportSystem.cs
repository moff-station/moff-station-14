using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Medical.Cryogenics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedCryoLifeSupportSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entity = default!;

    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoLifeSupportComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<CryoLifeSupportComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<CryoLifeSupportComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);

        Subs.BuiEvents<CryoLifeSupportComponent>(CryoLifeSupportUiKey.Key,
            subs =>
            {
                subs.Event<CryoLifeSupportSimpleUiMessage>(OnSimpleUiMessage);
                subs.Event<CryoLifeSupportInjectUiMessage>(OnInjectUiMessage);
            });
    }

    private void OnComponentInit(Entity<CryoLifeSupportComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.CapsuleSlotId, ent.Comp.CapsuleSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.BeakerSlotId, ent.Comp.BeakerSlot);

        // todo : probably need to add EnsureComp<CryoPodAir>(ent);
    }

    private void OnEntInsertedIntoContainer(Entity<CryoLifeSupportComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapsuleSlotId)
            return;

        AddComp<InsideCryoPodComponent>(ent);
        _appearance.SetData(ent.Owner, CryoLifeSupportVisuals.Filled, true);
        _ambient.SetAmbience(ent.Owner, true);
    }

    private void OnEntRemovedFromContainer(Entity<CryoLifeSupportComponent> ent,
        ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapsuleSlotId)
            return;

        _appearance.SetData(ent.Owner, CryoLifeSupportVisuals.Filled, false);
        _ambient.SetAmbience(ent.Owner, false);
    }


    private void OnSimpleUiMessage(Entity<CryoLifeSupportComponent> ent, ref CryoLifeSupportSimpleUiMessage args)
    {
        switch (args.Action)
        {
            case CryoLifeSupportSimpleUiMessage.ActionType.EjectCapsule :
                _itemSlots.TryEjectToHands(ent, ent.Comp.CapsuleSlot, args.Actor);
                _audio.PlayPredicted(ent.Comp.DetachCapsuleSound, ent, args.Actor);
                break;
            case CryoLifeSupportSimpleUiMessage.ActionType.EjectBeaker :
                _itemSlots.TryEjectToHands(ent, ent.Comp.BeakerSlot, args.Actor);
                break;
            case CryoLifeSupportSimpleUiMessage.ActionType.ReviveBrain :
                // probably send an Event to the capsule.
                _audio.PlayPredicted(ent.Comp.DetachCapsuleSound, ent, args.Actor);
                break;
            default:
                break;
        }
    }

    private void OnInjectUiMessage(Entity<CryoLifeSupportComponent> ent, ref CryoLifeSupportInjectUiMessage args)
    {
        // todo
    }
}


[Serializable, NetSerializable]
public enum CryoLifeSupportVisuals : byte
{
    Filled,
}
