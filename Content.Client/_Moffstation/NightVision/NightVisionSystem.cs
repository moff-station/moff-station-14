using Content.Client._Starlight.Overlays;
using Content.Client.Overlays;
using Content.Shared._Moffstation.NightVision;
using Content.Shared.Flash;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;

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

        // Just incase someone is editing it, they don't get to keep free night vision with flash protection
        if (_flash.IsFlashImmune(ent))
        {
            RemoveEffect(ent.Comp);
        }
    }

    private void OnFlashImmunityChanged(Entity<NightVisionComponent> ent, ref FlashImmunityChangedEvent args)
    {
        if (args.FlashImmune)
        {
            RemoveEffect(ent.Comp);
        }
        else
        {
            ApplyEffect(ent.Comp);
        }
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
            ApplyEffect(comp);
        }
    }

    private void OnShutDown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        RemoveEffect(ent.Comp);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void RemoveEffect(NightVisionComponent comp)
    {
        _overlayMan.RemoveOverlay(_overlay);
        Del(comp.Effect);
        comp.Effect = null;
    }

    private void ApplyEffect(NightVisionComponent comp)
    {
        _overlayMan.AddOverlay(_overlay);

        if (_player.LocalSession?.AttachedEntity is not { } player)
            return;

        if (comp.Effect != null)
            return;

        // Give them da light
        var effect = Spawn(comp.EffectPrototype, Transform(player).Coordinates);
        _transform.SetParent(effect, player);
        comp.Effect = effect;
    }
}
