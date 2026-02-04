using Content.Shared.Armor;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Armor;

/// Implements <see cref="SuitStorageAttachableComponent"/> & <see cref="SuitStorageAttachmentComponent"/>, allowing
/// customization of equipment to allow suit storage on clothing which otherwise would not allow it.
public sealed partial class SuitStorageAttachmentSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SuitStorageAttachableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SuitStorageAttachableComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<SuitStorageAttachableComponent, SuitStorageAttachmentAttachEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<SuitStorageAttachableComponent, SuitStorageAttachmentDetachEvent>(OnDetatchDoAfter);
        SubscribeLocalEvent<SuitStorageAttachableComponent, ExaminedEvent>(AttachableOnExamined);
        SubscribeLocalEvent<SuitStorageAttachableComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SuitStorageAttachableComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<SuitStorageAttachableComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);

        SubscribeLocalEvent<SuitStorageAttachmentComponent, ExaminedEvent>(AttachmentOnExamined);
    }

    /// Returns true if <paramref name="entity"/> has an attachment which allows storage of <paramref name="item"/> in
    /// suit storage. Returns false if <paramref name="entity"/> does not have a
    /// <see cref="SuitStorageAttachableComponent"/>, does not have an attachment, or if the attachment that it does
    /// have does not permit storage of <paramref name="item"/>.
    public bool IsEntityAllowedInSuitStorageByAttachment(
        Entity<SuitStorageAttachableComponent?> entity,
        EntityUid item
    )
    {
        if (!Resolve(entity, ref entity.Comp) ||
            GetSuitStorageAttachment((entity, entity.Comp)) is not { } attachment)
            return false;

        return !_whitelist.IsWhitelistFailOrNull(attachment.Comp.Whitelist, item);
    }

    private Entity<SuitStorageAttachmentComponent>? GetSuitStorageAttachment(Entity<SuitStorageAttachableComponent> ent)
    {
        if (ent.Comp.Slot.ContainedEntity is { } attachment &&
            TryComp<SuitStorageAttachmentComponent>(attachment, out var comp))
            return (attachment, comp);

        return null;
    }

    private void OnInit(Entity<SuitStorageAttachableComponent> ent, ref ComponentInit args)
    {
        // Innate suit storage allowance and attachable suit storage are incompatible on one entity.
        DebugTools.Assert(
            !HasComp<AllowSuitStorageComponent>(ent),
            $"Entity {ToPrettyString(ent)} has both {nameof(AllowSuitStorageComponent)} and {nameof(SuitStorageAttachableComponent)}. Entities with the former should not have the latter."
        );

        // Initialize the attachment container.
        ent.Comp.Slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.AttachmentSlotId);
    }

    /// Displays either "you can attach things to this!" or "this has $thing attached!".
    private void AttachableOnExamined(Entity<SuitStorageAttachableComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(
            GetSuitStorageAttachment(ent) is { } attachment
                ? Loc.GetString(ent.Comp.HasAttachmentText, ("attachment", attachment))
                : Loc.GetString(ent.Comp.CanAttachText)
        );
    }

    /// Displays "you can attach this to things!"
    private void AttachmentOnExamined(Entity<SuitStorageAttachmentComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.CanAttachText));
    }

    /// Adds the attach and detach verbs as appropriate.
    private void OnGetVerbs(Entity<SuitStorageAttachableComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanComplexInteract)
            return;

        var user = args.User;
        if (GetSuitStorageAttachment(ent) is { } attachment)
        {
            // Detach verb
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(ent.Comp.DetachVerbName),
                Icon = ent.Comp.DetachIcon,
                Act = () => _doAfter.TryStartDoAfter(
                    new DoAfterArgs(
                        EntityManager,
                        user,
                        attachment.Comp.AttachDelay * ent.Comp.AttachDelayModifier,
                        new SuitStorageAttachmentDetachEvent(),
                        ent,
                        target: ent
                    )
                    {
                        BreakOnDamage = true,
                        BreakOnMove = true,
                        NeedHand = true,
                        RequireCanInteract = true,
                    }
                ),
            });
        }
        else if (TryComp<SuitStorageAttachmentComponent>(args.Using, out var attachmentComp))
        {
            // Attach verb
            var used = args.Using;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString(ent.Comp.AttachVerbName),
                Icon = ent.Comp.AttachIcon,
                Disabled = HasComp<AllowSuitStorageComponent>(ent),
                Act = () => _doAfter.TryStartDoAfter(
                    new DoAfterArgs(
                        EntityManager,
                        user,
                        attachmentComp.AttachDelay * ent.Comp.AttachDelayModifier,
                        new SuitStorageAttachmentAttachEvent(),
                        ent,
                        target: ent,
                        used: used
                    )
                    {
                        BreakOnDamage = true,
                        BreakOnMove = true,
                        NeedHand = true,
                        BreakOnDropItem = true,
                        BreakOnHandChange = true,
                        RequireCanInteract = true,
                    }
                ),
            });
        }
    }

    /// Spits out any attachments stored in the attachable slot.
    private void OnDestruction(Entity<SuitStorageAttachableComponent> ent, ref DestructionEventArgs args)
    {
        _container.EmptyContainer(ent.Comp.Slot, destination: Transform(ent).Coordinates);
    }

    /// Rejects attaching things which are not attachments.
    private void OnInsertAttempt(Entity<SuitStorageAttachableComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled ||
            args.Container != ent.Comp.Slot || // Don't do anything if the container isn't the one we manage.
            HasComp<SuitStorageAttachmentComponent>(args.EntityUid))
            return;

        args.Cancel();
    }

    /// Actually handles attaching, which just means inserting the attachment into the slot.
    private void OnAttachDoAfter(Entity<SuitStorageAttachableComponent> ent, ref SuitStorageAttachmentAttachEvent args)
    {
        if (args.Cancelled ||
            args.Used is not { } used)
            return;

        _container.Insert(used, ent.Comp.Slot);
        args.Handled = true;
    }

    /// Handles detaching, which involves removing the attachment from the slot and putting the attachment into the
    /// actor's hand.
    private void OnDetatchDoAfter(Entity<SuitStorageAttachableComponent> ent, ref SuitStorageAttachmentDetachEvent args)
    {
        if (args.Cancelled ||
            ent.Comp.Slot.ContainedEntity is not { } attachment)
            return;

        _container.Remove(attachment, ent.Comp.Slot);
        _hands.TryPickupAnyHand(args.User, attachment);
        args.Handled = true;
    }

    /// Whenever an attachment is removed, unequip whatever's in suit storage.
    private void OnEntRemovedFromContainer(
        Entity<SuitStorageAttachableComponent> ent,
        ref EntRemovedFromContainerMessage args
    )
    {
        if (!HasComp<SuitStorageAttachmentComponent>(args.Entity))
            return;

        _inventory.TryUnequip(Transform(ent).ParentUid, "suitstorage", force: true, checkDoafter: false);
    }
}
