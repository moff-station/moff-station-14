using System.Text;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Clothing;

public sealed class ExaminableClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExaminableClothingComponent, InventoryRelayedEvent<ExaminedEvent>>(OnExaminedWorn);
    }

    public string ExamineText(Entity<ExaminableClothingComponent> ent, EntityUid wearer)
    {
        var textList = new List<string>();

        if (ent.Comp.ExamineText is { } examineText)
            textList.Add(Loc.GetString("examinable-clothing-examine", ("wearer", wearer), ("item", Loc.GetString(examineText, ("wearer", wearer)))));

        if (ent.Comp.ExtraText is { } extraText)
            textList.Add(Loc.GetString(extraText, ("wearer", wearer)));

        return string.Join("\n", textList);
    }

    private void OnExaminedWorn(Entity<ExaminableClothingComponent> ent, ref InventoryRelayedEvent<ExaminedEvent> args)
    {
        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot) || (slot.SlotFlags & ent.Comp.AllowedSlots) == SlotFlags.NONE)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        args.Args.PushMarkup(ExamineText(ent, container.Owner));
    }
}
