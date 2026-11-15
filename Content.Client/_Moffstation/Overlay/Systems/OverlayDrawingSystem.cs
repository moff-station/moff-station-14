using Content.Client._Moffstation.Overlay.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Moffstation.Overlay.Systems;

/// <summary>
/// This is a system handling the drawing of basic elements inside an overlay.
///
/// </summary>
public sealed partial class OverlayDrawingSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _xform = default!;

    [SubscribeLocalEvent]
    private void OnGetDrawingFunctions(Entity<SpriteComponent> ent, ref GetOverlayDrawingFunctionsEvent ev)
    {
        ev.Functions.Add(DrawSprite);
    }

    private void DrawSprite(EntityUid ent, in OverlayDrawArgs args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // todo : figure out what to do if sprite is noRot.
        _sprite.RenderSprite((ent, sprite),
            args.WorldHandle,
            Angle.Zero,
            _xform.GetWorldRotation(ent),
            _xform.GetWorldPosition(ent));
    }
}
