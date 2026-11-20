using Content.Client._Moffstation.Overlay.Events;
using Content.Client.Popups;
using Content.Shared._Moffstation.Physics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Moffstation.ArInterface.Systems;

public sealed partial class ArInterfaceSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;

    [SubscribeLocalEvent]
    private void OnDrawSprite(Entity<SpriteComponent> ent, ref ArDrawEvent e)
    {
        _sprite.RenderSprite(ent,
            e.WorldHandle,
            _eyeManager.CurrentEye.Rotation,
            _xform.GetWorldRotation(ent),
            _xform.GetWorldPosition(ent));
    }

    // todo : for some reason this function sometime draw nothing at all,
    //        sometime add an unexistant offset to posA/posB before drawing.
    //        I have not a single clue on why this happen.
    [SubscribeLocalEvent]
    private void OnDrawLinks(Entity<VisualLinksComponent> ent, ref ArDrawEvent e)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(ent, out var xformA))
            return;

        var texture = _sprite.Frame0(ent.Comp.LinkSprite);
        var width = texture.Width / (float)EyeManager.PixelsPerMeter;

        var posA = _xform.GetWorldPosition(xformA); //+ _xform.GetWorldRotation(ent).RotateVec(ent.Comp.OffsetA);

        foreach (var target in ent.Comp.Targets)
        {
            if (!xformQuery.TryGetComponent(target, out var xformB) || xformB.MapID != xformA.MapID)
                continue;

            var posB = _xform.GetWorldPosition(xformB); //+ _xform.GetWorldRotation(target).RotateVec(ent.Comp.OffsetB);

            var diff =  posB - posA;
            var length =  diff.Length();
            var angle = diff.ToWorldAngle();
            var mid = (posA + posB) / 2f;

            var box = new Box2(-width/2f, -length/2f, width/2f, length/2f);
            var rotate = new Box2Rotated(box.Translated(mid), angle, mid);

            e.WorldHandle.DrawTextureRect(texture, rotate);
        }
    }
}
