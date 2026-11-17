using System.Numerics;
using Content.Client._Moffstation.Overlay.Events;
using Content.Shared._Moffstation.ArInterface.Components;

namespace Content.Client._Moffstation.ArInterface.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class ArInterfaceSystem
{
    [SubscribeLocalEvent]
    private void OnSpawn(Entity<ArElementComponent> ent, ref ArSpawnEvent e)
    {
        switch (ent.Comp.SpawnAt)
        {
            case ArSpawnAtViewer v:
                _xform.SetParent(ent, e.ViewerEntity);
                _xform.SetLocalPosition(ent, v.Offset);
                _xform.SetLocalRotation(ent, v.Angle);
                break;
            case ArSpawnAtSource s:
                _xform.SetParent(ent, e.SourceEntity);
                _xform.SetLocalPosition(ent, s.Offset);
                _xform.SetLocalRotation(ent, s.Angle);
                break;
            default:
                _xform.SetParent(ent, e.SourceEntity);
                _xform.SetLocalPosition(ent, Vector2.Zero);
                _xform.SetLocalRotation(ent, 0);
                break;
        }

        _sprite.SetVisible(ent.Owner, false);
    }
}
