using Content.Server._Moffstation.Remotes.Components;
using Content.Server.Power;
using Content.Server.Wires;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Moffstation.Remotes.EntitySystems;

public sealed partial class PulseRemoteSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnRangeInteract(Entity<PulseRemoteComponent> ent, ref BeforeRangedInteractEvent ev)
    {
        if (ev.Handled || ev.Target is not { } target ||
            !TryComp<WiresComponent>(target, out var wires) || !ent.Comp.AcceptedLayouts.Contains(wires.LayoutId) ||
            !_examine.InRangeUnOccluded(ev.User, target, ent.Comp.Range, ent.Comp.IgnoreOcclusion? _ => true : null))
            return;

        ev.Handled = true;

        var success = false;
        foreach (var wire in wires.WiresList)
        {
            if (wire.Action is not PowerWireAction { } powerWire)
                continue;

            if (!success && !_charges.TryUseCharge(ent.Owner))
                break;

            success = true;
            powerWire.Pulse(ent, wire);
        }

        if (success)
            _audio.PlayEntity(ent.Comp.PulseSound, ev.User, ent);
    }
}
