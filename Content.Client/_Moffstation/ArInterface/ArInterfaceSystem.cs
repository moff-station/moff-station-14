using Content.Client._Moffstation.Overlay.Events;
using Content.Shared._Moffstation.Physics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Moffstation.ArInterface;

public sealed partial class ArInterfaceSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _xform = default!;


    [SubscribeLocalEvent]
    private void OnDraw(Entity<SpriteComponent> ent, ref ArDrawEvent e)
    {
        _sprite.RenderSprite(ent,
            e.HandleWorld,
            Angle.Zero,
            _xform.GetWorldRotation(ent),
            _xform.GetWorldPosition(ent));
    }

    [SubscribeLocalEvent]
    private void OnDraw(Entity<VisualLinksComponent> ent, ref ArDrawEvent e)
    {
        var texture = _sprite.Frame0(ent.Comp.Sprite);
        var width = texture.Width / (float)EyeManager.PixelsPerMeter;

        var mapId = _xform.GetMapId(ent.Owner);

        var posA = _xform.GetWorldPosition(ent) + _xform.GetWorldRotation(ent).RotateVec(ent.Comp.OffsetA);

        // if you want some fun see what Rider what you to do with Linq, here
        foreach (var target in ent.Comp.Targets)
        {
            if (_xform.GetMapId(target) != mapId)
                continue;

            var posB = _xform.GetWorldPosition(target) + _xform.GetWorldRotation(target).RotateVec(ent.Comp.OffsetB);

            var diff =  posB - posA;
            var length =  diff.Length();
            var angle = diff.ToWorldAngle();
            var mid = (posA + posB) / 2;

            var box = new Box2(-width/2f, -length/2f, width/2f, length/2f);
            var rotate = new Box2Rotated(box.Translated(mid), angle, mid);

            e.HandleWorld.DrawTextureRect(texture, rotate);
        }
    }
}
