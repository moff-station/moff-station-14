using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryocapsuleSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryocapsuleComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<CryocapsuleComponent, ComponentInit>(OnCryocapsuleInit);

        SubscribeLocalEvent<CryocapsuleComponent, OrganInsertedIntoEvent>(OnOrganInsertedInto);
        SubscribeLocalEvent<CryocapsuleComponent, OrganRemovedFromEvent>(OnOrganRemovedFrom);
    }

    private void OnCryocapsuleInit(Entity<CryocapsuleComponent> ent, ref ComponentInit init)
    {
        EnsureComp<BodyComponent>(ent);
    }

    private void OnInteractUsing(Entity<CryocapsuleComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BodyComponent>(ent, out var body) ||
            body.Organs is not { } _ ||
            !TryComp<OrganComponent>(args.Used, out var organ) ||
            organ.Category is not { } category ||
            !ent.Comp.OrganWhitelist.Contains(category))
            return;

        if (!ent.Comp.Organs.TryGetValue(category, out var present))
        {
            _container.Insert(args.Used, body.Organs);
        }
        else
        {
            // swap with the organ already present.
        }

        ent.Comp.Organs[category] = args.Used;
        args.Handled = true;
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



    private void OnOrganInsertedInto(Entity<CryocapsuleComponent> ent, ref OrganInsertedIntoEvent args)
    {
        // the layer corresponding to this organ become visible.
        if (!TryComp<BodyComponent>(ent, out var body))
            return;
    }

    private void OnOrganRemovedFrom(Entity<CryocapsuleComponent> ent, ref OrganRemovedFromEvent args)
    {
        // the layer corresponding to this organ become hidden.
    }


    public CryoCapsuleEntry GenerateCryocapsuleEntry(Entity<CryocapsuleComponent> ent)
    {
        return new CryoCapsuleEntry(
            false,
            false,
            false,
            false,
            false
        );
    }
}
