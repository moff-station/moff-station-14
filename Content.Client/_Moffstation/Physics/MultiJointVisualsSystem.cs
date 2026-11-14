using System.Numerics;
using Content.Client._Moffstation.Overlay.Events;
using Content.Shared._Moffstation.Physics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Moffstation.Physics;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class MultiJointVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _xform = default!;

    [SubscribeLocalEvent]
    private void OnGetDrawingFunction(Entity<MultiJointVisualsComponent> ent, ref GetOverlayDrawingFunctionsEvent ev)
    {
        ev.Functions.Add(DrawJoints);
    }


    private void DrawJoints(EntityUid ent, in OverlayDrawArgs args)
    {
        if (! TryComp<MultiJointVisualsComponent>(ent, out var comp))
            return;

        var mapId = _xform.GetMapId(ent);
        var posA = _xform.GetWorldPosition(ent) + _xform.GetWorldRotation(ent).RotateVec(comp.SourceOffset);
        
        var texture = _sprite.Frame0(comp.Sprite);
        var width = texture.Width / (float)EyeManager.PixelsPerMeter;

        foreach (var tar in comp.Targets)
        {
            if (tar is not {} target|| _xform.GetMapId(target) != mapId)
                continue;

            var posB = _xform.GetWorldPosition(target) + _xform.GetWorldRotation(target).RotateVec(comp.TargetOffset);

            var length = (posB - posA).Length();
            var angle = Angle.FromWorldVec(posB - posA);
            var midpoint = (posA + posB) / 2f;

            var box = new Box2(-width / 2f, -length / 2f, width / 2f, length / 2f);
            var rotate = new Box2Rotated(box.Translated(midpoint), angle, midpoint);

            args.WorldHandle.DrawTextureRect(texture, rotate);
        }
    }
}
