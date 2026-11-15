using System.Linq;
using System.Numerics;
using Content.Client._Moffstation.Overlay.Events;
using Content.Shared._Moffstation.ArInterface.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.ArInterface;

public sealed partial class ArDisplaysSystem : EntitySystem
{
    [Dependency] private TransformSystem _xform = default!;

    [SubscribeLocalEvent]
    private void OnUpdate(Entity<ArDisplaysComponent> ent, ref ArDisplayUpdateEvent args)
    {
        var ev = new ArSpawnEvent(ent, args.ViewerEntity);

        var toAdd = ent.Comp.Displays.Keys
            .Intersect(args.Interfaces)
            .Except(ent.Comp.Entities.Keys);
        foreach (var addedInterface in toAdd)
        {
            ent.Comp.Entities.Add(addedInterface, []);
            foreach (var proto in ent.Comp.Displays[addedInterface])
            {
                var display = Spawn(proto);
                RaiseLocalEvent(display, ref ev);
                ent.Comp.Entities[addedInterface].Add(display);
            }
        }

        var toRemove = ent.Comp.Entities.Keys.Except(args.Interfaces);
        foreach (var removedInterface in toRemove)
        {
            foreach (var display in ent.Comp.Entities[removedInterface])
            {
                Del(display);
            }
            ent.Comp.Entities.Remove(removedInterface);
        }
    }

    [SubscribeLocalEvent]
    private void OnSpawn(Entity<ArElementComponent> ent, ref ArSpawnEvent args)
    {
        switch (ent.Comp.SpawnAt)
        {
            case ArSpawnAtViewer v:
                _xform.SetParent(ent, args.ViewerEntity);
                _xform.SetLocalPosition(ent, v.Offset);
                _xform.SetLocalRotation(ent, v.Angle);
                break;
            case ArSpawnAtSource s:
                _xform.SetParent(ent, args.SourceEntity);
                _xform.SetLocalPosition(ent, s.Offset);
                _xform.SetLocalRotation(ent, s.Angle);
                break;
            default:
                _xform.SetParent(ent, args.SourceEntity);
                _xform.SetLocalPosition(ent, Vector2.Zero);
                _xform.SetLocalRotation(ent, 0);
                break;
        }
    }
}
