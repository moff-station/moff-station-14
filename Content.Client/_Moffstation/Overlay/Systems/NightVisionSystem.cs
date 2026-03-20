using System.Numerics;
using Content.Client._Starlight.Overlays;
using Content.Client.Overlays;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared.Flash;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Moffstation.Overlay.Systems;

/// <summary>
/// This system implements the behavior of <see cref="NightVisionComponent"/>.
/// </summary>
public sealed class NightVisionSystem : EquipmentHudSystem<NightVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;

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
            _overlayMan.RemoveOverlay(_overlay);

        TryComp<PointLightComponent>(ent.Owner, out var light);
        if (!light?.Enabled ?? false) // skip this option if we have no PointLightComponent
            _pointLightSystem.SetEnabled(ent.Owner, true, light);
    }

    private void OnFlashImmunityChanged(Entity<NightVisionComponent> ent, ref FlashImmunityChangedEvent args)
    {
        if (args.FlashImmune)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
        else
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<NightVisionComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var comp in component.Components)
        {
            _overlay.TintColor = comp.TintColor;
            _overlay.TintIntensity = comp.TintIntensity;
        }

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutDown(Entity<NightVisionComponent> ent, ref ComponentShutdown args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
