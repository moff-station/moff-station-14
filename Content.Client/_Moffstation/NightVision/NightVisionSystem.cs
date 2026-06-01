using Content.Client._Starlight.Overlays;
using Content.Client.Overlays;
using Content.Shared._Moffstation.NightVision;
using Content.Shared.Flash;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._Moffstation.NightVision;

/// <summary>
/// This system implements the behavior of <see cref="NightVisionComponent"/>.
/// </summary>
public sealed partial class NightVisionSystem : EquipmentHudSystem<NightVisionComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private SharedFlashSystem _flash = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, FlashImmunityChangedEvent>(OnFlashImmunityChanged);
        SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnShutDown);

        _overlay = new();
    }

    private void OnHandleState(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    private void OnFlashImmunityChanged(Entity<NightVisionComponent> ent, ref FlashImmunityChangedEvent args)
    {
        var localPlayer = _player.LocalSession?.AttachedEntity;
        if (ent.Owner != localPlayer && Transform(ent.Owner).ParentUid != localPlayer)
            return;

        if (args.FlashImmune)
            RemoveEffect(ent.Comp);
        else
            ApplyEffect(ent.Comp);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> component)
    {
        base.UpdateInternal(component);

        if (_player.LocalSession?.AttachedEntity is not { } player)
            return;

        foreach (var comp in component.Components)
        {
            _overlay.TintColor = comp.TintColor;
            _overlay.TintIntensity = comp.TintIntensity;
        }

        foreach (var comp in component.Components)
        {
            UpdateEffect(player, comp);
        }
    }

    private void OnShutDown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        RemoveEffect(ent.Comp);
    }

    protected override void OnCompUnequip(Entity<NightVisionComponent> ent, ref GotUnequippedEvent args)
    {
        base.OnCompUnequip(ent, ref args);
        if (args.EquipTarget != _player.LocalSession?.AttachedEntity)
            return;
        RemoveEffect(ent.Comp);
    }

    protected override void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        base.OnPlayerDetached(args);

        if (TryComp<NightVisionComponent>(args.Entity, out var comp))
            RemoveEffect(comp);

        var enumerator = _inventory.GetSlotEnumerator(args.Entity);
        while (enumerator.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } item && TryComp<NightVisionComponent>(item, out var equipComp))
                RemoveEffect(equipComp);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void UpdateEffect(EntityUid user, NightVisionComponent comp)
    {
        if (_flash.IsFlashImmune(user))
        {
            RemoveEffect(comp);
        }
        else
        {
            ApplyEffect(comp);
        }
    }

    private void RemoveEffect(NightVisionComponent comp)
    {
        _overlayMan.RemoveOverlay(_overlay);
        Del(comp.Effect);
        // Sometimes this was failing to delete, so I have a check here so we dont accidentally lose track
        if (Deleted(comp.Effect))
            comp.Effect = null;
    }

    private void ApplyEffect(NightVisionComponent comp)
    {
        _overlayMan.AddOverlay(_overlay);

        if (_player.LocalSession?.AttachedEntity is not { } player)
            return;

        // We dont want to lose track of previous effects if theyre still around
        if (comp.Effect != null)
            return;

        // Give them da light
        var effect = Spawn(comp.EffectPrototype, Transform(player).Coordinates);
        _transform.SetParent(effect, player);
        comp.Effect = effect;
    }
}
