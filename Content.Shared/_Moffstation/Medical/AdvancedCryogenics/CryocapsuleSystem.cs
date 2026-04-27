using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

// TODO : this will probably go inside the Cryomachine system.
// TODO : get better organs reading (status...)

/// <summary>
/// This handles...
/// </summary>
public sealed class CryocapsuleSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ExposedOrgansSystem _organs = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryocapsuleComponent, ComponentInit>(OnCryocapsuleInit);
    }

    private void OnCryocapsuleInit(Entity<CryocapsuleComponent> ent, ref ComponentInit init)
    {
        EnsureComp<ExposedOrgansComponent>(ent);
    }


    public CryoCapsuleEntry GenerateCryocapsuleEntry(Entity<CryocapsuleComponent> ent)
    {
        return new CryoCapsuleEntry(
            true,
            false,
            false,
            false,
            false
        );
    }



    public bool TryGetBrain(Entity<CryocapsuleComponent> ent, [NotNullWhen(true)] out Entity<BrainComponent>? brain)
    {
        brain = null;
        /*
        if (!ent.Comp.BrainSlot.HasItem ||
            ent.Comp.BrainSlot.Item is not { } brainEnt ||
            ! TryComp<BrainComponent>(brainEnt, out var brainComp))
            return false;

        brain = (brainEnt, brainComp);
        return true;
        */
        return false;
    }
}
