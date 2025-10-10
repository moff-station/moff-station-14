using Content.Client._Starlight.Overlay;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared.Flash;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Overlay.Systems;

/// <summary>
/// This system implements the behavior of <see cref="NightVisionComponent"/>.
/// </summary>
public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<NightVisionComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<NightVisionComponent, FlashImmunityChangedEvent>(OnFlashImmunityChanged);
    }

    private void OnFlashImmunityChanged(Entity<NightVisionComponent> ent, ref FlashImmunityChangedEvent args)
    {
        if (args.FlashImmune)
        {
            RemoveEffect(ent);
        }
        else
        {
            ApplyEffect(ent);
        }
    }

    private void OnPlayerAttached(Entity<NightVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        ApplyEffect(ent);
    }

    private void OnPlayerDetached(Entity<NightVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveEffect(ent, true);
    }

    private void OnVisionInit(Entity<NightVisionComponent> ent, ref ComponentInit args)
    {
        ApplyEffect(ent);
    }

    private void OnVisionShutdown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        RemoveEffect(ent);
    }

    private void ApplyEffect(Entity<NightVisionComponent> entity)
    {
        if (entity.Comp.Effect != null ||
            _player.LocalSession?.AttachedEntity != entity ||
            _flash.IsFlashImmune(entity))
            return;

        _overlayMan.AddOverlay(new NightVisionOverlay());
        var effect = SpawnAttachedTo(entity.Comp.EffectPrototype, Transform(entity).Coordinates);
        _xformSys.SetParent(effect, entity);
        entity.Comp.Effect = effect;
    }

    // `force` is needed because we need to remove the overlay AFTER the player is detached.
    private void RemoveEffect(Entity<NightVisionComponent> entity, bool force = false)
    {
        if (!force &&
            _player.LocalSession?.AttachedEntity != entity)
            return;

        if (!_overlayMan.TryGetOverlay(out NightVisionOverlay? overlay))
            return;

        _overlayMan.RemoveOverlay(overlay);
        PredictedQueueDel(entity.Comp.Effect);
        entity.Comp.Effect = null;
    }
}
