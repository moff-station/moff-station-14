using Content.Shared._Moffstation.ArInterface.Components;
using Content.Shared._Moffstation.Physics;
using Content.Shared._Moffstation.SightDetection;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared._Moffstation.ArInterface.Systems;

public sealed partial class ArSightIndicatorSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnSighted(Entity<ArSightIndicatorComponent> ent, ref DetectorSeeTargetEvent ev)
    {
        if (!TryComp<VisualLinksComponent>(ent, out var links))
            return;

        links.Targets.Add(ev.Target);
    }

    [SubscribeLocalEvent]
    private void OnUnsighted(Entity<ArSightIndicatorComponent> ent, ref DetectorUnseeTargetEvent ev)
    {
        if (!TryComp<VisualLinksComponent>(ent, out var links))
            return;

        links.Targets.Remove(ev.Target);
    }
}
