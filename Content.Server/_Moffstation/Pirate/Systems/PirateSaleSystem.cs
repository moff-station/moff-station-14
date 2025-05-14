using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Moffstation.Pirate.Components;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.Cargo.Components;
using Content.Shared._Moffstation.Pirate.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Pirate.Systems;

public sealed partial class PirateSaleSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateShuttleComponent, MapInitEvent>(OnMapInit);

        // Bounty console
        SubscribeLocalEvent<PirateBountyConsoleComponent, BoundUIOpenedEvent>(OnBountyConsoleOpened);
        SubscribeLocalEvent<PirateBountyConsoleComponent, BountyPrintLabelMessage>(OnPrintLabelMessage);
        SubscribeLocalEvent<PirateBountyConsoleComponent, BountySkipMessage>(OnSkipBountyMessage);

        // Kinda order console
        SubscribeLocalEvent<PirateOrderConsoleComponent, CargoConsoleWithdrawFundsMessage>(OnWithdrawFunds);

        // Order console
        SubscribeLocalEvent<PirateOrderConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
        SubscribeLocalEvent<PirateOrderConsoleComponent, CargoConsoleRemoveOrderMessage>(OnRemoveOrderMessage);
        SubscribeLocalEvent<PirateOrderConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);
        SubscribeLocalEvent<PirateOrderConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
        SubscribeLocalEvent<PirateOrderConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PirateOrderConsoleComponent, InteractUsingEvent>(OnInteractUsing);
    }

    #region BountyConsole

    private void OnBountyConsoleOpened(Entity<PirateBountyConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        var shuttle = _transform.GetGrid(ent.Owner.ToCoordinates());
        if(!TryComp<StationCargoBountyDatabaseComponent>(shuttle, out var bountyDb))
            return;

        var untilNextSkip = bountyDb.NextSkipTime - _gameTiming.CurTime;
        _uiSystem.SetUiState(ent.Owner, CargoConsoleUiKey.Bounty, new CargoBountyConsoleState(bountyDb.Bounties, bountyDb.History, untilNextSkip));
    }

    private void OnPrintLabelMessage(Entity<PirateBountyConsoleComponent> ent, ref BountyPrintLabelMessage args)
    {
        if (!TryComp<CargoBountyConsoleComponent>(ent.Owner, out var cargoBountyComp))
            return;
        if (_gameTiming.CurTime < cargoBountyComp.NextPrintTime)
            return;

        var shuttle = _transform.GetGrid(ent.Owner.ToCoordinates());
        if (shuttle == null)
            return;

        if (!_cargoSystem.TryGetBountyFromId((EntityUid)shuttle, args.BountyId, out var bounty))
            return;

        var label = Spawn(cargoBountyComp.BountyLabelId, Transform(ent.Owner).Coordinates);
        cargoBountyComp.NextPrintTime = _gameTiming.CurTime + cargoBountyComp.PrintDelay;
        _cargoSystem.SetupBountyLabel(label, (EntityUid)shuttle, bounty.Value);
        _audio.PlayPvs(cargoBountyComp.PrintSound, ent.Owner);
    }

    private void OnSkipBountyMessage(Entity<PirateBountyConsoleComponent> ent, ref BountySkipMessage args)
    {
        var shuttle = _transform.GetGrid(ent.Owner.ToCoordinates());
        if (shuttle == null)
            return;

        if(!TryComp<StationCargoBountyDatabaseComponent>(shuttle, out var db))
            return;

        if (_gameTiming.CurTime < db.NextSkipTime)
            return;

        if (!_cargoSystem.TryGetBountyFromId((EntityUid) shuttle, args.BountyId, out var bounty))
            return;

        if (args.Actor is not { Valid: true } mob)
            return;

        if (!TryComp<CargoBountyConsoleComponent>(ent.Owner, out var cargoBountyComp))
            return;

        if (TryComp<AccessReaderComponent>(ent.Owner, out var accessReaderComponent) &&
            !_accessReaderSystem.IsAllowed(mob, ent.Owner, accessReaderComponent))
        {
            _audio.PlayPvs(cargoBountyComp.DenySound, ent.Owner);
            return;
        }

        if (!_cargoSystem.TryRemoveBounty((EntityUid) shuttle, bounty.Value, true, args.Actor))
            return;

        FillBountyDatabase((EntityUid) shuttle);
        db.NextSkipTime = _gameTiming.CurTime + db.SkipDelay;
        var untilNextSkip = db.NextSkipTime - _gameTiming.CurTime;
        _uiSystem.SetUiState(ent.Owner, CargoConsoleUiKey.Bounty, new CargoBountyConsoleState(db.Bounties, db.History, untilNextSkip));
        _audio.PlayPvs(cargoBountyComp.SkipSound, ent.Owner);
    }

    private void OnMapInit(Entity<PirateShuttleComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<StationCargoBountyDatabaseComponent>(ent.Owner, out var bountyDb))
            return;
        EmptyBountyDatabase(ent.Owner, bountyDb);
        FillBountyDatabase(ent.Owner, bountyDb);
    }

    public void EmptyBountyDatabase(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        component.Bounties = new List<CargoBountyData>();
    }

    /// <summary>
    /// Fills up the bounty database with random bounties.
    /// </summary>
    public void FillBountyDatabase(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        while (component.Bounties.Count < component.MaxBounties)
        {
            if (!TryAddBounty(uid, component))
                break;
        }

        _cargoSystem.UpdateBountyConsoles();
    }

    [PublicAPI]
    public bool TryAddBounty(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // todo: consider making the cargo bounties weighted.
        var allBounties = _protoMan.EnumeratePrototypes<CargoBountyPrototype>().ToList();
        var filteredBounties = new List<CargoBountyPrototype>();
        foreach (var proto in allBounties)
        {
            if (proto.Secret != "Pirates")
                continue;
            if (component.Bounties.Any(b => b.Bounty == proto.ID))
                continue;
            filteredBounties.Add(proto);
        }

        var pool = filteredBounties.Count == 0 ? allBounties : filteredBounties;
        var bounty = _random.Pick(pool);
        return _cargoSystem.TryAddBounty(uid, bounty, component);
    }

    private void OnWithdrawFunds(Entity<PirateOrderConsoleComponent> ent, ref CargoConsoleWithdrawFundsMessage args)
    {
        if (!GetShuttleComp(ent.Owner, out var shuttle) || shuttle == null)
            return;

        if (args.Amount <= 0 || args.Amount > shuttle.Money)
            return;

        if (!TryComp<CargoOrderConsoleComponent>(ent.Owner, out var cargoOrderConsoleComponent))
            return;
        if (_gameTiming.CurTime < cargoOrderConsoleComponent.NextAccountActionTime)
            return;

        if (!_accessReaderSystem.IsAllowed(args.Actor, ent))
        {
            _popup.PopupCursor(Loc.GetString("cargo-console-order-not-allowed"), args.Actor);
            _audio.PlayPvs(_audio.ResolveSound(cargoOrderConsoleComponent.ErrorSound), ent.Owner);
        }

        shuttle.Money -= args.Amount;
        _audio.PlayPvs(ApproveSound, ent);

        var stackPrototype = _protoMan.Index(cargoOrderConsoleComponent.CashType);
        _stack.Spawn(args.Amount, stackPrototype, Transform(ent).Coordinates);
    }

    #endregion

    #region OrderConsole

    private void OnInit(Entity<PirateOrderConsoleComponent> ent, ref ComponentInit args)
    {
        var shuttle = _transform.GetGrid(ent.Owner.ToCoordinates());
        _cargoSystem.UpdateOrderState(ent.Owner, shuttle);
    }

    private void OnInteractUsing(Entity<PirateOrderConsoleComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<CashComponent>(args.Used))
            return;

        var price = _pricing.GetPrice(args.Used);

        if (price == 0)
            return;

        if (!GetShuttleComp(ent.Owner, out var shuttle) || shuttle == null)
            return;

        _audio.PlayPvs(ApproveSound, ent.Owner);
        shuttle.Money += price;
        QueueDel(args.Used);
        args.Handled = true;
    }

    private void OnAddOrderMessage(Entity<PirateOrderConsoleComponent> ent, ref CargoConsoleAddOrderMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (args.Amount <= 0)
            return;

        var shuttle = _transform.GetGrid(ent.Owner.ToCoordinates());
        if (shuttle == null)
            return;

        if (!_cargoSystem.TryGetOrderDatabase(shuttle, out var orderDatabase))
            return;

        if (!_protoMan.TryIndex<CargoProductPrototype>(args.CargoProductId, out var product))
        {
            Log.Error($"Tried to add invalid cargo product {args.CargoProductId} as order!");
            return;
        }

        if (!TryComp<CargoOrderConsoleComponent>(ent.Owner, out var cargoOrderConsoleComponent))
            return;

        var productInGroup = false;
        foreach (var group in cargoOrderConsoleComponent.AllowedGroups)
        {
            if (group == product.Group)
                productInGroup = true;
        }

        if (!productInGroup)
            return;

        var data = new CargoOrderData(GenerateOrderId(orderDatabase), product.Product, product.Name, product.Cost, args.Amount, args.Requester, args.Reason);

        if (!TryAddOrder((EntityUid)shuttle, cargoOrderConsoleComponent.Account, data, orderDatabase))
        {
            _audio.PlayPvs(_audio.ResolveSound(cargoOrderConsoleComponent.ErrorSound), ent.Owner);
        }
    }

    private void OnRemoveOrderMessage(Entity<PirateOrderConsoleComponent> ent, ref CargoConsoleRemoveOrderMessage args)
    {
        if (!TryGetPirateShuttle(ent.Owner, out var shuttle))
            return;

        if (!TryGetOrderDatabase(shuttle, out var orderDatabase))
            return;

        if (!TryComp<CargoOrderConsoleComponent>(ent.Owner, out var cargoOrderConsoleComponent))
            return;

        _cargoSystem.RemoveOrder(shuttle.Value, cargoOrderConsoleComponent.Account, args.OrderId, orderDatabase);
    }

    private void OnApproveOrderMessage(Entity<PirateOrderConsoleComponent> ent, ref CargoConsoleApproveOrderMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        // Get the normal component
        if (!TryComp<CargoOrderConsoleComponent>(ent.Owner, out var cargoOrderConsoleComponent))
            return;

        if (!_accessReaderSystem.IsAllowed(player, ent.Owner))
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            _audio.PlayPvs(_audio.ResolveSound(cargoOrderConsoleComponent.ErrorSound), ent.Owner);
            return;
        }

        // get the shuttle
        if (!TryGetPirateShuttle(ent.Owner, out var shuttle))
            return;

        // No station to deduct from.
        if (!TryGetOrderDatabase(shuttle, out var orderDatabase))
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-station-not-found"));
            _audio.PlayPvs(_audio.ResolveSound(cargoOrderConsoleComponent.ErrorSound), ent.Owner);
            return;
        }

        // Find our order again. It might have been dispatched or approved already
        var order = new CargoOrderData();
        foreach (var currentOrder in orderDatabase.Orders[cargoOrderConsoleComponent.Account])
        {
            if (args.OrderId == currentOrder.OrderId && !currentOrder.Approved)
            {
                order = currentOrder;
                break;
            }
        }
        if (order == new CargoOrderData())
            return;

        // Invalid order
        if (!_protoMan.HasIndex<EntityPrototype>(order.ProductId))
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-product"));
            _cargoSystem.PlayDenySound(ent.Owner, cargoOrderConsoleComponent);
            return;
        }

        var amount = 0;
        foreach (var currentOrder in orderDatabase.Orders[cargoOrderConsoleComponent.Account])
        {
            if (!currentOrder.Approved)
                continue;
            amount += currentOrder.OrderQuantity - currentOrder.NumDispatched;
        }

        var capacity = orderDatabase.Capacity;

        // Too many orders, avoid them getting spammed in the UI.
        if (amount >= capacity)
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-too-many"));
            _cargoSystem.PlayDenySound(ent.Owner, cargoOrderConsoleComponent);
            return;
        }

        // Cap orders so someone can't spam thousands.
        var cappedAmount = Math.Min(capacity - amount, order.OrderQuantity);

        if (cappedAmount != order.OrderQuantity)
        {
            order.OrderQuantity = cappedAmount;
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-snip-snip"));
            _cargoSystem.PlayDenySound(ent.Owner, cargoOrderConsoleComponent);
        }

        var cost = order.Price * order.OrderQuantity;
        if (!GetShuttleComp(ent.Owner, out var shuttleComp) || shuttleComp == null)
            return;

        // Not enough balance
        if (cost > shuttleComp.Money)
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
            _cargoSystem.PlayDenySound(ent.Owner, cargoOrderConsoleComponent);
            return;
        }

        var ev = new FulfillPirateOrderEvent((shuttle.Value, shuttleComp), order, (ent.Owner, cargoOrderConsoleComponent));
        RaiseLocalEvent(ref ev);
        ev.FulfillmentEntity ??= shuttle.Value;

        if (!ev.Handled)
        {
            //Rather than do that crazy fallback, for pirates we'll just spawn stuff on the console
            //It won't work if it's not on their shuttle anyways
            ev.FulfillmentEntity = ent.Owner;

            if (ev.FulfillmentEntity == null)
            {
                _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
                _cargoSystem.PlayDenySound(ent.Owner, cargoOrderConsoleComponent);
                return;
            }
        }

        order.Approved = true;
        _audio.PlayPvs(ApproveSound, ent.Owner);

        if (!_emag.CheckFlag(ent.Owner, EmagType.Interaction))
        {
            var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent.Owner, player);
            RaiseLocalEvent(tryGetIdentityShortInfoEvent);
            order.SetApproverData(tryGetIdentityShortInfoEvent.Title);

            var message = Loc.GetString("cargo-console-unlock-approved-order-broadcast",
                ("productName", Loc.GetString(order.ProductName)),
                ("orderAmount", order.OrderQuantity),
                ("approver", order.Approver ?? string.Empty),
                ("cost", cost));
            if (cargoOrderConsoleComponent.AnnouncementsEnabled)
            {
                _radio.SendRadioMessage(ent.Owner, message, cargoOrderConsoleComponent.AnnouncementChannel, ent.Owner, escapeMarkup: false);
                if (CargoOrderConsoleComponent.BaseAnnouncementChannel != cargoOrderConsoleComponent.AnnouncementChannel)
                    _radio.SendRadioMessage(ent.Owner, message, CargoOrderConsoleComponent.BaseAnnouncementChannel, ent.Owner, escapeMarkup: false);
            }
        }

        _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-trade-station", ("destination", MetaData(ev.FulfillmentEntity.Value).EntityName)));

        orderDatabase.Orders[cargoOrderConsoleComponent.Account].Remove(order);
        shuttleComp.Money -= cost;
        UpdateOrders(shuttle.Value);
    }

    private void OnOrderUIOpened(EntityUid uid, PirateOrderConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!TryGetPirateShuttle(uid, out var shuttle))
            return;
        _cargoSystem.UpdateOrderState(uid, shuttle);
    }

    private bool TryAddOrder(EntityUid dbUid, ProtoId<CargoAccountPrototype> account, CargoOrderData data, StationCargoOrderDatabaseComponent component)
    {
        component.Orders[account].Add(data);
        UpdateOrders(dbUid);
        return true;
    }

    public bool TryGetOrderDatabase([NotNullWhen(true)] EntityUid? stationUid, [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp)
    {
        return TryComp(stationUid, out dbComp);
    }

    /// <summary>
    /// Updates all of the cargo-related consoles for a particular station.
    /// This should be called whenever orders change.
    /// </summary>
    private void UpdateOrders(EntityUid dbUid)
    {
        // Order added so all consoles need updating.
        var orderQuery = AllEntityQuery<CargoOrderConsoleComponent>();

        while (orderQuery.MoveNext(out var uid, out var _))
        {
            var shuttle = _transform.GetGrid(uid);
            if (shuttle == null)
                continue;
            if (shuttle != dbUid)
                continue;

            _cargoSystem.UpdateOrderState(uid, shuttle);
        }
    }

    private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
    {
        // We need an arbitrary unique ID to identify orders, since they may
        // want to be cancelled later.
        return ++orderDB.NumOrdersCreated;
    }

    #endregion

    #region Shared

    public bool GetShuttleComp(EntityUid uid, out PirateShuttleComponent? shuttleComp)
    {
        shuttleComp = null;
        if (TryGetPirateShuttle(uid, out var shuttle) || shuttle == null)
            return false;
        if (!TryComp(uid, out shuttleComp))
            return false;
        return true;
    }

    private bool TryGetPirateShuttle(EntityUid uid, out EntityUid? shuttle)
    {
        shuttle = _transform.GetGrid(uid.ToCoordinates());
        if (shuttle == null)
            return false;
        return true;
    }

    public int GetBalanceFromAccount(Entity<StationBankAccountComponent?> station, ProtoId<CargoAccountPrototype> account)
    {
        if (!Resolve(station, ref station.Comp))
            return 0;

        return station.Comp.Accounts.GetValueOrDefault(account);
    }

    #endregion
}
