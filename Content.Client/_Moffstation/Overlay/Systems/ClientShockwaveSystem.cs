using Content.Client._Moffstation.Overlay.Overlays;
using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared._Moffstation.Overlay.EntitySystems;
using Robust.Client.Graphics;


namespace Content.Client._Moffstation.Overlay.Systems;

/// <summary>
/// This handles the ShockwaveComponent which interfaces with the ShockwaveOverlay
/// </summary>
public sealed partial class ClientShockwaveSystem : SharedShockwaveSystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ShockwaveOverlay _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ShockwaveOverlay();

        SubscribeLocalEvent<ShockwaveComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ShockwaveComponent, ComponentShutdown>(OnCompShutdown);
    }


    private void OnCompInit(Entity<ShockwaveComponent> entity, ref ComponentInit args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnCompShutdown(Entity<ShockwaveComponent> entity, ref ComponentShutdown args)
    {
        _overlayMan.RemoveOverlay<ShockwaveOverlay>();
    }
}
