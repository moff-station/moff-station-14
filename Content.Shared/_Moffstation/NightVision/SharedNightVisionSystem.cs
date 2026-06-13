using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared._Moffstation.NightVision;

public sealed partial class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RefreshEquipmentHudEvent<NightVisionComponent>>(OnBodyRefreshHud);
        SubscribeLocalEvent<NightVisionComponent, BodyRelayedEvent<RefreshEquipmentHudEvent<NightVisionComponent>>>(OnBodyRelayedRefreshHud);

        // Relay for worn items
        SubscribeLocalEvent<NightVisionComponent, GotEquippedEvent>(OnNVItemEquipped);
        SubscribeLocalEvent<NightVisionComponent, GotUnequippedEvent>(OnNVItemUnequipped);

        // Relay for organs
        SubscribeLocalEvent<NightVisionComponent, OrganGotInsertedEvent>(OnNVOrganInserted);
        SubscribeLocalEvent<NightVisionComponent, OrganGotRemovedEvent>(OnNVOrganRemoved);
    }

    private void OnBodyRefreshHud(Entity<BodyComponent> ent, ref RefreshEquipmentHudEvent<NightVisionComponent> args)
    {
        _body.RelayEvent(ent, ref args);
    }

    private void OnBodyRelayedRefreshHud(Entity<NightVisionComponent> ent, ref BodyRelayedEvent<RefreshEquipmentHudEvent<NightVisionComponent>> args)
    {
        var ev = args.Args;
        ev.Active = true;
        ev.Components.Add(ent.Comp);
        args = args with { Args = ev };
    }

    private void OnNVItemEquipped(Entity<NightVisionComponent> ent, ref GotEquippedEvent args)
    {
        var ev = new RefreshEquipmentHudEvent<NightVisionComponent>(~SlotFlags.POCKET);
        RaiseLocalEvent(args.EquipTarget, ref ev);
    }

    private void OnNVItemUnequipped(Entity<NightVisionComponent> ent, ref GotUnequippedEvent args)
    {
        var ev = new RefreshEquipmentHudEvent<NightVisionComponent>(~SlotFlags.POCKET);
        RaiseLocalEvent(args.EquipTarget, ref ev);
    }

    private void OnNVOrganInserted(Entity<NightVisionComponent> ent, ref OrganGotInsertedEvent args)
    {
        var ev = new RefreshEquipmentHudEvent<NightVisionComponent>(~SlotFlags.POCKET);
        RaiseLocalEvent(args.Target, ref ev);
    }

    private void OnNVOrganRemoved(Entity<NightVisionComponent> ent, ref OrganGotRemovedEvent args)
    {
        var ev = new RefreshEquipmentHudEvent<NightVisionComponent>(~SlotFlags.POCKET);
        RaiseLocalEvent(args.Target, ref ev);
    }
}
