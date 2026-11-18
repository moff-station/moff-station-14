using Content.Server._Moffstation.Remotes.Components;
using Content.Server.Power;
using Content.Server.Silicons.StationAi;
using Content.Server.Wires;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Moffstation.Remotes.EntitySystems;

public sealed partial class HackerRemoteSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnRangeInteract(Entity<HackerRemoteComponent> ent, ref BeforeRangedInteractEvent ev)
    {
        if (ev.Handled || ev.Target is not { } target ||
            !TryComp<WiresComponent>(target, out var wires) || !ent.Comp.AcceptedLayouts.Contains(wires.LayoutId) ||
            !_examine.InRangeUnOccluded(ev.User, target, ent.Comp.Range, ent.Comp.IgnoreOcclusion? _ => true : null))
            return;

        ev.Handled = true;

        var used = false;
        foreach (var wire in wires.WiresList)
        {
            switch (wire.Action)
            {
                case PowerWireAction power :
                    if (!used && !_charges.TryUseCharge(ent.Owner))
                        return;
                    power.Pulse(ent, wire);
                    break;
                case AiVisionWireAction ai :
                    if (!used && !_charges.TryUseCharge(ent.Owner))
                        return;
                    ai.Pulse(ent, wire);
                    break;
                default:
                    continue;
            }

            used = true;
        }

        _audio.PlayEntity(ent.Comp.PulseSound, ev.User, ent);
    }
}
