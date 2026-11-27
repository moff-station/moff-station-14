using Content.Server.Power;
using Content.Server.Wires;
using Content.Shared.Charges.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Moffstation.Remotes;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class HackingRemoteSystem : EntitySystem
{
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnBeforeInteract(Entity<HackingRemoteComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target ||
            _charges.IsEmpty(ent.Owner) ||
            !_examine.InRangeUnOccluded(args.User, target, ent.Comp.MaxRange, (_) => true) ||
            !TryComp<WiresComponent>(target, out var wires) ||
            ! ent.Comp.Layouts.Contains(wires.LayoutId))
            return;

        args.Handled = true;

        if (!_powerReceiver.IsPowered(target))
            return;

        foreach (var wire in wires.WiresList)
        {
            if (wire.Action is PowerWireAction action)
            {
                action.Pulse(ent, wire);
                //action.Pulse(args.User, wire);
                _charges.TryUseCharge(ent.Owner);
                _audio.PlayEntity(ent.Comp.BeepSound, args.User, ent);
                break;
            }
        }
    }
}
