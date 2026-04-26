using System.Linq;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Mind;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
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
    [Dependency] protected readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] protected readonly IGameTiming _time = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] protected readonly CryocapsuleSystem _cryoCapsule = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;


    /// <inheritdoc/>

    // TODO : Issue with exception system.user_interface: UI Key got BoundInterfaceMessageWrapMessage from a client who was not subscribed
    //        (when you go away from the machine with the UI still open)

    // TODO : Find a way to make the capsule don't go through the machine when it's ejected.
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

    private void OnCryomachineInit(Entity<CryomachineComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.CapsuleSlotId, ent.Comp.CapsuleSlot);
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.BeakerSlotId, ent.Comp.BeakerSlot);
    }

    private void OnInserted(Entity<CryomachineComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapsuleSlotId)
            return;

        _appearance.SetData(ent.Owner, CryomachineVisuals.Filled, true);
        _ambient.SetAmbience(ent.Owner, true);
    }

    private void OnRemoved(Entity<CryomachineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapsuleSlotId)
            return;

        _appearance.SetData(ent.Owner, CryomachineVisuals.Filled, false);
        _ambient.SetAmbience(ent.Owner, false);
    }

    private void OnSimpleUiMessage(Entity<CryomachineComponent> ent, ref CryomachineSimpleUiMessage args)
    {
        switch (args.Type)
        {
            case CryomachineSimpleUiMessage.MessageType.JumpstartBrain :
                ReviveBrain(ent);
                _audio.PlayPredicted(ent.Comp.ShockSound, ent.Owner, ent.Owner, AudioParams.Default);
                break;
            case CryomachineSimpleUiMessage.MessageType.DetachCapsule :
                _audio.PlayPredicted(ent.Comp.DetachSound, ent.Owner, ent.Owner, AudioParams.Default);
                if (ent.Comp.CapsuleSlot is { HasItem: true, Item: { } capsule })
                    _itemSlots.TryEject(capsule, ent.Comp.CapsuleSlot, null, out _);
                break;
            case CryomachineSimpleUiMessage.MessageType.EjectBeaker:
                break;
        }
    }


    private void ReviveBrain(Entity<CryomachineComponent> ent)
    {
        if (ent.Comp.CapsuleSlot.Item is not { } capsule ||
            !TryComp<CryocapsuleComponent>(capsule, out var capsuleComp) ||
            !_cryoCapsule.TryGetBrain((capsule, capsuleComp), out var brain))
            return;

        // bad way to say that but will probably be changed later.
        if (brain is {} entity && _mind.TryGetMind(entity, out var mindId, out var mindComp))
        {
            _mind.TransferTo(mindId, capsule, true, mind:mindComp);
        }
    }


    protected (FixedPoint2? capacity, List<ReagentQuantity>? reagents) GetBeakerInfo(Entity<CryomachineComponent> ent)
    {
        if (ent.Comp.BeakerSlot.Item is not { } beaker ||
            ! TryComp<SolutionContainerManagerComponent>(beaker, out var containerComp) ||
            ! TryComp<FitsInDispenserComponent>(beaker, out var dispenserComp) ||
            ! _solutionContainer.TryGetFitsInDispenser((beaker,  dispenserComp, containerComp), out var solutionComp, out _))
            return (null, null);

        var capacity = solutionComp.Value.Comp.Solution.MaxVolume;
        var reagents = solutionComp.Value.Comp.Solution.Contents
            .Select(reagent =>new ReagentQuantity(reagent.Reagent, reagent.Quantity))
            .ToList();

        return (capacity, reagents);
    }
}

[Serializable, NetSerializable]
public enum CryomachineVisuals : byte
{
    Filled,
}
