using Content.Client._Starlight.Overlay;
using Content.Shared._Moffstation.NightVision;
using Content.Shared.Flash;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.NightVision;

/// This system implements the behavior of <see cref="NightVisionComponent"/>.
public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;

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
        // Prevent effect flickering due to rollbacks.
        if (!_gameTiming.IsFirstTimePredicted)
            return;

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
        RemoveEffect(ent);
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

        // "Enqueue" activation of night vision.
        EnsureComp<DirtyNightVisionComponent>(entity);
    }

    private void RemoveEffect(Entity<NightVisionComponent> entity)
    {
        _overlayMan.RemoveOverlay<NightVisionOverlay>();
        QueueDel(entity.Comp.Effect);
        entity.Comp.Effect = null;
        RemCompDeferred<DirtyNightVisionComponent>(entity);
    }

    public override void Update(float frameTime)
    {
        // We use the marker+update pattern for changing here due a pernicious intermittent issue where night vision
        // would be turned on while an entity was being deleted as part of round-end which would cause
        // https://github.com/moff-station/moff-station-14/issues/1293 (or something like it) due to adding entities
        // during state application, which iterates over existing entities.
        var q = AllEntityQuery<NightVisionComponent, DirtyNightVisionComponent>();
        while (q.MoveNext(out var ent, out var comp, out _))
        {
            if (comp.Effect != null)
                continue;

            _overlayMan.AddOverlay(new NightVisionOverlay());
            var effect = SpawnAttachedTo(comp.EffectPrototype, Transform(ent).Coordinates);
            _xformSys.SetParent(effect, ent);
            comp.Effect = effect;
            RemCompDeferred<DirtyNightVisionComponent>(ent);
        }
    }
}
