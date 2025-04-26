using Content.Server._Moffstation.Cargo.Events;
using Content.Server.Cargo.Components;
using Content.Server.Station.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.IdentityManagement;
using Robust.Shared.Prototypes;

// Not Content.Server._Moffstation... because this is partial code for an existing system.
namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, CargoApprovedOrderMessage>(OnApprovedOrderMessage);
    }

    private void OnApprovedOrderMessage(
        Entity<StationCargoOrderDatabaseComponent> entity,
        ref CargoApprovedOrderMessage args
    )
    {
        if (!TryComp<StationBankAccountComponent>(entity, out var bank))
        {
            // TODO CENT
            throw new NotImplementedException();
        }

        // Find our order again. It might have been dispatched or approved already
        var orderId = args.OrderId;
        var order = entity.Comp.Orders[args.Account]
            .Find(order => orderId == order.OrderId && !order.Approved);
        if (order == null)
        {
            return;
        }

        // Invalid order
        if (!_protoMan.HasIndex<EntityPrototype>(order.ProductId))
        {
            args.DenialReason = Loc.GetString("cargo-console-invalid-product");
            return;
        }

        var amount = GetOutstandingOrderCount(entity.Comp, args.Account);

        // Too many orders, avoid them getting spammed in the UI.
        if (amount >= entity.Comp.Capacity)
        {
            args.DenialReason = Loc.GetString("cargo-console-too-many");
            return;
        }

        // Cap orders so someone can't spam thousands.
        var cappedAmount = Math.Min(entity.Comp.Capacity - amount, order.OrderQuantity);

        if (cappedAmount != order.OrderQuantity)
        {
            order.OrderQuantity = cappedAmount;
            args.DenialReason = Loc.GetString("cargo-console-snip-snip");
        }

        var cost = order.Price * order.OrderQuantity;
        var accountBalance = GetBalanceFromAccount((entity, bank), args.Account);

        // Not enough balance
        if (cost > accountBalance)
        {
            args.DenialReason = Loc.GetString("cargo-console-insufficient-funds", ("cost", cost));
            return;
        }

        var ev = new FulfillCargoOrderEvent(order, entity);
        RaiseLocalEvent(ref ev);

        if (!ev.Handled)
        {
            if (GetMoneyServerStation(entity) is { } station)
            {
                ev.FulfillmentEntity = TryFulfillOrder(station, order, entity.Comp);
            }

            if (ev.FulfillmentEntity == null)
            {
                args.DenialReason = Loc.GetString("cargo-console-unfulfilled");
                return;
            }
        }

        order.Approved = true;

        if (args.ShouldAnnounceFulfillment)
        {
            var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(args.Approver, args.ApprovedOnDevice);
            RaiseLocalEvent(tryGetIdentityShortInfoEvent);
            order.SetApproverData(tryGetIdentityShortInfoEvent.Title);

            // TODO CENT The money server entity needs radio stuff
            var message = Loc.GetString("cargo-console-unlock-approved-order-broadcast",
                ("productName", Loc.GetString(order.ProductName)),
                ("orderAmount", order.OrderQuantity),
                ("approver", order.Approver ?? string.Empty),
                ("cost", cost));
            _radio.SendRadioMessage(entity, message, args.AnnouncementChannel, entity, escapeMarkup: false);
            if (CargoOrderConsoleComponent.BaseAnnouncementChannel != args.AnnouncementChannel)
            {
                _radio.SendRadioMessage(entity,
                    message,
                    CargoOrderConsoleComponent.BaseAnnouncementChannel,
                    entity,
                    escapeMarkup: false);
            }
        }

        ConsolePopup(args.Approver,
            Loc.GetString("cargo-console-trade-station",
                ("destination", MetaData(ev.FulfillmentEntity!.Value).EntityName)));

        // Log order approval
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Approver):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}] on account {args.Account} with balance at {accountBalance}");

        entity.Comp.Orders[args.Account].Remove(order);
        UpdateBankAccount((entity, bank), -cost, CreateAccountDistribution(args.Account, bank));
        UpdateOrders();
    }

    private Entity<StationBankAccountComponent, StationCargoOrderDatabaseComponent>? GetLinkedCargoServer(
        EntityUid client)
    {
        if (!TryComp<DeviceLinkSinkComponent>(client, out var sink))
            return null;

        foreach (var source in sink.LinkedSources)
        {
            if (TryComp<StationBankAccountComponent>(source, out var bank) &&
                TryComp<StationCargoOrderDatabaseComponent>(source, out var orders))
                return (source, bank, orders);
        }

        return null;
    }

    private Entity<StationDataComponent>? GetMoneyServerStation(EntityUid server)
    {
        if (_station.GetOwningStation(server) is { } stationEnt &&
            TryComp<StationDataComponent>(stationEnt, out var stationData))
        {
            return (stationEnt, stationData);
        }

        return null;
    }
}
