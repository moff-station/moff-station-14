using Content.Client._Starlight.Overlays;
using Content.Shared._Moffstation.NightVision;
using Content.Shared.Body;
using Content.Shared.Flash;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Moffstation.NightVision;

/// <summary>
/// This system implements the behavior of <see cref="NightVisionComponent"/>.
/// </summary>
public sealed partial class NightVisionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private TransformSystem _xformSys = default!;
    [Dependency] private SharedFlashSystem _flash = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnVisionShutdown);
        SubscribeLocalEvent<NightVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);

        // Global (not component-filtered) so we can scan inventory for equipped NV items
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<NightVisionComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NightVisionComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<NightVisionComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<NightVisionComponent, OrganGotRemovedEvent>(OnOrganRemoved);

        SubscribeLocalEvent<NightVisionComponent, FlashImmunityChangedEvent>(OnFlashImmunityChanged);
    }

    private void OnHandleState(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var localPlayer = _player.LocalSession?.AttachedEntity;
        if (localPlayer == null)
            return;
        if (!IsOwnedByLocalPlayer(ent.Owner, localPlayer.Value))
            return;

        if (ent.Comp.Enabled && !_flash.IsFlashImmune(localPlayer.Value))
            ApplyEffect(ent);
        else
            RemoveEffect(ent);
    }

    private void OnFlashImmunityChanged(Entity<NightVisionComponent> ent, ref FlashImmunityChangedEvent args)
    {
        var localPlayer = _player.LocalSession?.AttachedEntity;
        if (localPlayer == null)
            return;
        if (!IsOwnedByLocalPlayer(ent.Owner, localPlayer.Value))
            return;

        if (args.FlashImmune)
            RemoveEffect(ent);
        else
            ApplyEffect(ent);
    }

    private void OnEquipped(Entity<NightVisionComponent> ent, ref GotEquippedEvent args)
    {
        if (args.EquipTarget != _player.LocalSession?.AttachedEntity)
            return;
        ApplyEffect(ent);
    }

    private void OnUnequipped(Entity<NightVisionComponent> ent, ref GotUnequippedEvent args)
    {
        if (args.EquipTarget != _player.LocalSession?.AttachedEntity)
            return;
        RemoveEffect(ent);
    }

    private void OnOrganInserted(Entity<NightVisionComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (args.Target != _player.LocalSession?.AttachedEntity)
            return;
        ApplyEffect(ent);
    }

    private void OnOrganRemoved(Entity<NightVisionComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (args.Target != _player.LocalSession?.AttachedEntity)
            return;
        RemoveEffect(ent);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (TryComp<NightVisionComponent>(args.Entity, out var comp))
            ApplyEffect((args.Entity, comp));

        var enumerator = _inventory.GetSlotEnumerator(args.Entity);
        while (enumerator.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } item && TryComp<NightVisionComponent>(item, out var equipComp))
                ApplyEffect((item, equipComp));
        }

        if (TryComp<BodyComponent>(args.Entity, out var body))
        {
            foreach (var organ in body.Organs?.ContainedEntities ?? [])
            {
                if (TryComp<NightVisionComponent>(organ, out var organComp))
                    ApplyEffect((organ, organComp));
            }
        }
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (TryComp<NightVisionComponent>(args.Entity, out var comp))
            RemoveEffect((args.Entity, comp));

        var enumerator = _inventory.GetSlotEnumerator(args.Entity);
        while (enumerator.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } item && TryComp<NightVisionComponent>(item, out var equipComp))
                RemoveEffect((item, equipComp));
        }

        if (TryComp<BodyComponent>(args.Entity, out var body))
        {
            foreach (var organ in body.Organs?.ContainedEntities ?? [])
            {
                if (TryComp<NightVisionComponent>(organ, out var organComp))
                    RemoveEffect((organ, organComp));
            }
        }
    }

    private void OnVisionInit(Entity<NightVisionComponent> ent, ref ComponentInit args)
    {
        ApplyEffect(ent);
    }

    private void OnVisionShutdown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        RemoveEffect(ent);
    }

    private bool IsOwnedByLocalPlayer(EntityUid entity, EntityUid localPlayer)
    {
        if (entity == localPlayer)
            return true;
        if (Transform(entity).ParentUid == localPlayer)
            return true;
        if (TryComp<BodyComponent>(localPlayer, out var body) && (body.Organs?.Contains(entity) ?? false))
            return true;
        return false;
    }

    private void ApplyEffect(Entity<NightVisionComponent> entity)
    {
        var localPlayer = _player.LocalSession?.AttachedEntity;
        if (localPlayer == null)
            return;
        if (!IsOwnedByLocalPlayer(entity.Owner, localPlayer.Value))
            return;
        if (!entity.Comp.Enabled || _flash.IsFlashImmune(localPlayer.Value))
            return;

        _overlay.TintColor = entity.Comp.TintColor;
        _overlay.TintIntensity = entity.Comp.TintIntensity;
        _overlayMan.AddOverlay(_overlay);

        if (entity.Comp.Effect != null)
            return;

        var effect = Spawn(entity.Comp.EffectPrototype, Transform(localPlayer.Value).Coordinates);
        _xformSys.SetParent(effect, localPlayer.Value);
        entity.Comp.Effect = effect;
    }

    private void RemoveEffect(Entity<NightVisionComponent> entity)
    {
        _overlayMan.RemoveOverlay(_overlay);
        Del(entity.Comp.Effect);
        // Sometimes this was failing to delete, so I have a check here so we dont accidentally lose track
        if (Deleted(entity.Comp.Effect))
            entity.Comp.Effect = null;
    }
}