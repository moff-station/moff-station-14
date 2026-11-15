using System.Linq;
using System.Numerics;
using Content.Shared._Moffstation.ArInterface.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.ArInterface;

public sealed partial class ArDisplaysSystem : EntitySystem
{
    [Dependency] private TransformSystem _xform = default!;

    [SubscribeLocalEvent]
    private void OnUpdate(Entity<ArDisplaysComponent> ent, ref ArDisplayUpdateEvent args)
    {
        var toAdd = ent.Comp.Displays.Keys
            .Intersect(args.Interfaces)
            .Except(ent.Comp.Entities.Keys);
        foreach (var addedInterface in toAdd)
        {
            ent.Comp.Entities.Add(addedInterface, []);
            foreach (var proto in ent.Comp.Displays[addedInterface])
            {
                var display = Spawn(proto);
                _xform.SetParent(display, ent);
                _xform.SetLocalPosition(display, Vector2.Zero);
                _xform.SetLocalRotation(display, 0);
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
}
