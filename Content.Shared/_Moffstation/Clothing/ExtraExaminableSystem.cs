using System.Text;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Clothing;

public sealed class ExtraExaminableSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraExaminableComponent, InventoryRelayedEvent<ExaminedEvent>>(OnExaminedWorn);
    }

    private void OnExamined(Entity<ExtraExaminableComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("examinable-clothing-when-worn", ("message", ExamineText(ent, args.Examiner))));
    }

    public string ExamineText(Entity<ExtraExaminableComponent> ent, EntityUid wearer)
    {
        var textList = new List<string>();

        if (ent.Comp.WornText is { } examineText)
            textList.Add(Loc.GetString("examinable-clothing-examine", ("wearer", wearer), ("item", Loc.GetString(examineText, ("wearer", wearer)))));

        if (ent.Comp.ExtraText is { } extraText)
            textList.Add(Loc.GetString(extraText, ("wearer", wearer)));

        return string.Join("\n", textList);
    }

    private void OnExaminedWorn(Entity<ExtraExaminableComponent> ent, ref InventoryRelayedEvent<ExaminedEvent> args)
    {
        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot) || (slot.SlotFlags & ent.Comp.AllowedSlots) == SlotFlags.NONE)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        args.Args.PushMarkup(ExamineText(ent, container.Owner));
    }
}
