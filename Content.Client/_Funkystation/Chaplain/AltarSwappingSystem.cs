using Content.Shared._Funkystation.Chaplain;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Client._Starfall.Particles;
using Content.Shared._Starfall.Particles;

namespace Content.Client._Funkystation.Chaplain;

public sealed partial class AltarSwappingSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ParticleSystem _particles = default!;

    private static readonly ProtoId<ParticleEffectPrototype> HolyEffect = "AltarSwapBurst";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltarSwappingComponent, AfterAutoHandleStateEvent>(OnComponentState);
        SubscribeNetworkEvent<AltarSwappedEffectEvent>(OnAltarSwappedEffect);
    }

    private void OnComponentState(Entity<AltarSwappingComponent> altar, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(altar);
    }

    private void UpdateAppearance(Entity<AltarSwappingComponent> altar)
    {
        if (!_prototypes.TryIndex(altar.Comp.CurrentStyle, out EntityPrototype? prototype))
            return;

        if (!TryComp(altar, out SpriteComponent? sprite))
            return;

        if (!prototype.TryGetComponent(out SpriteComponent? sourceSprite, Factory))
            return;

        // I know this is obsolete I have no idea how I'm supposed to appropriately use CopySprite here
        sprite.CopyFrom(sourceSprite);
    }

    // Plays the particle effect when the altar is swapped
    // this is a network event because the server is the one that decides when the altar is swapped
    private void OnAltarSwappedEffect(AltarSwappedEffectEvent args)
    {
        var altar = GetEntity(args.Altar);

        if  (!Exists(altar))
            return;

        _particles.CreateParticle(HolyEffect, altar, attach: false);
    }
}
