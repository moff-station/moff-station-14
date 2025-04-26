using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void InitializeTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CargoTelepadComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        // Shouldn't need re-anchored event
        SubscribeLocalEvent<CargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
        SubscribeLocalEvent<FulfillCargoOrderEvent>(OnTelepadFulfillCargoOrder);
    }

    private void OnTelepadFulfillCargoOrder(ref FulfillCargoOrderEvent args)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tele, out var xform))
        {
            if (tele.CurrentState != CargoTelepadState.Idle)
                continue;

            if (!this.IsPowered(uid, EntityManager))
                continue;

            // Moffstation - Start - Explicit linkage to cargo server means this is unnecessary
            // if (_station.GetOwningStation(uid, xform) != args.Station)
            //     continue;
            // Moffstation - End

            // todo cannot be fucking asked to figure out device linking rn but this shouldn't just default to the first port.
            // Moffstation - Start - Cargo Server
            if (!TryGetLinkedOrderDb((uid, tele), out var console) ||
                console.Value.Owner != args.Source.Owner)
                // Moffstation - End
                continue;

            for (var i = 0; i < args.Order.OrderQuantity; i++)
            {
                tele.CurrentOrders.Add(args.Order);
            }
            tele.Accumulator = tele.Delay;
            args.Handled = true;
            args.FulfillmentEntity = uid;
            return;
        }
    }

    // Moffstation - Start - Cargo Server
    private bool TryGetLinkedOrderDb(Entity<CargoTelepadComponent> ent,
        [NotNullWhen(true)] out Entity<StationCargoOrderDatabaseComponent>? orders)
    {
        orders = null;
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sinkComponent) ||
            sinkComponent.LinkedSources.FirstOrNull() is not { } linked ||
            !TryComp<StationCargoOrderDatabaseComponent>(linked, out var orderDbComp))
            return false;

        orders = (linked, orderDbComp);
        // Moffstation - End
        return true;
    }


    private void UpdateTelepad(float frameTime)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // Don't EntityQuery for it as it's not required.
            TryComp<AppearanceComponent>(uid, out var appearance);

            if (comp.CurrentState == CargoTelepadState.Unpowered)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                comp.Accumulator = comp.Delay;
                continue;
            }

            comp.Accumulator -= frameTime;

            // Uhh listen teleporting takes time and I just want the 1 float.
            if (comp.Accumulator > 0f)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                continue;
            }

            if (comp.CurrentOrders.Count == 0 || !TryGetLinkedOrderDb((uid, comp), out var console)) // Moffstation - Cargo Server
            {
                comp.Accumulator += comp.Delay;
                continue;
            }

            var currentOrder = comp.CurrentOrders.First();
            if (FulfillOrder(currentOrder, xform.Coordinates, comp.PrinterOutput)) // Moffstation - Cargo Server
            {
                _audio.PlayPvs(_audio.ResolveSound(comp.TeleportSound), uid, AudioParams.Default.WithVolume(-8f));

                UpdateOrders(); // Moffstation - Cargo Server

                comp.CurrentOrders.Remove(currentOrder);
                comp.CurrentState = CargoTelepadState.Teleporting;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Teleporting, appearance);
            }

            comp.Accumulator += comp.Delay;
        }
    }

    private void OnInit(EntityUid uid, CargoTelepadComponent telepad, ComponentInit args)
    {
        _linker.EnsureSinkPorts(uid, telepad.ReceiverPort);
    }

    private void OnShutdown(Entity<CargoTelepadComponent> ent, ref ComponentShutdown args)
    {
        // Moffstation - Start - Cargo Server
        if (ent.Comp.CurrentOrders.Count == 0 ||
            GetLinkedCargoServer(ent) is not { } server ||
            GetMoneyServerStation(server) is not { } station ||
            !TryGetLinkedOrderDb(ent, out var console))
            // Moffstation - End
            return;

        foreach (var order in ent.Comp.CurrentOrders)
        {
            TryFulfillOrder(station, order, server); // Moffstation - Cargo Server
        }
    }

    private void SetEnabled(EntityUid uid, CargoTelepadComponent component, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(uid, ref receiver, ref xform, false))
            return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Setting idle state should be handled by Update();
        if (disabled)
            return;

        TryComp<AppearanceComponent>(uid, out var appearance);
        component.CurrentState = CargoTelepadState.Unpowered;
        _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Unpowered, appearance);
    }

    private void OnTelepadPowerChange(EntityUid uid, CargoTelepadComponent component, ref PowerChangedEvent args)
    {
        SetEnabled(uid, component);
    }

    private void OnTelepadAnchorChange(EntityUid uid, CargoTelepadComponent component, ref AnchorStateChangedEvent args)
    {
        SetEnabled(uid, component);
    }
}
