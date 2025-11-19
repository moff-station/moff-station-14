using Linq;
using Content.Shared.Verbs;
using Content.Shared._Moffstation.PizzaScurret.Receipt;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.PizzaScurret.DeliveryBag;

/// <summary>
///  This system handles printing the receipt, and bomb logic for the delivery bag.
/// </summary>
public sealed class DeliveryBagSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeliveryBagComponent, PrintReceiptMessage>(OnPrintReceipt);
    }

private void PrintReceipt(Entity<DeliveryBagComponent> ent, EntityUid customer)
    {
                if (!task.Item.Validate())
                    return;
                if (_timing.CurTime < ent.Comp.NextPrintAllowedAfter)
                    return;

                ent.Comp.NextPrintAllowedAfter = _timing.CurTime + ent.Comp.PrintDelay;
                var printed = Spawn("PaperNanoTaskItem", Transform(message.Actor).Coordinates);
                _hands.PickupOrDrop(message.Actor, printed);
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/printer.ogg"), ent.Owner);
                SetupPrintedTask(printed, task.Item);
                break;

                args.Verbs.Add(verb); // Print the receipt using verb.
    }
}
