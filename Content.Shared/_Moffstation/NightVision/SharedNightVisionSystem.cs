using Content.Shared.Body;
using Content.Shared.Inventory.Events;
using Content.Shared.Toggleable;

namespace Content.Shared._Moffstation.NightVision;

public sealed partial class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RefreshEquipmentHudEvent<NightVisionComponent>>(OnBodyRefreshHud);
        SubscribeLocalEvent<NightVisionComponent, BodyRelayedEvent<RefreshEquipmentHudEvent<NightVisionComponent>>>(OnBodyRelayedRefreshHud);
    }

    private void OnBodyRefreshHud(Entity<BodyComponent> ent, ref RefreshEquipmentHudEvent<NightVisionComponent> args)
    {
        _body.RelayEvent(ent, ref args);
    }

    private void OnBodyRelayedRefreshHud(Entity<NightVisionComponent> ent, ref BodyRelayedEvent<RefreshEquipmentHudEvent<NightVisionComponent>> args)
    {
        args.Args.Components.Add(ent.Comp);
    }
}
