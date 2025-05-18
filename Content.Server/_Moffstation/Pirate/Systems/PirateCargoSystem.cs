using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Moffstation.Pirate.Components;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.Cargo.Components;
using Content.Shared._Moffstation.Cargo.Events;
using Content.Shared._Moffstation.Pirate.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.Pirate.Systems;

public sealed partial class PirateCargoSystem : EntitySystem
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
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<CargoSellBlacklistComponent> _blacklistQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<TradeStationComponent> _tradeQuery;

    private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg");

    private HashSet<EntityUid> _setEnts = new();
    private List<EntityUid> _listEnts = new();
    private List<(EntityUid, PiratePalletComponent, TransformComponent)> _pads = new();

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _blacklistQuery = GetEntityQuery<CargoSellBlacklistComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _tradeQuery = GetEntityQuery<TradeStationComponent>();

        // Shuttle
        SubscribeLocalEvent<PirateShuttleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PirateShuttleComponent, GridSplitEvent>(OnPirateShuttleSplit);

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

        //Sale Console
        SubscribeLocalEvent<PiratePalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);
        SubscribeLocalEvent<PiratePalletConsoleComponent, CargoPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<PiratePalletConsoleComponent, CargoPalletAppraiseMessage>(OnPalletAppraise);
    }

    private void OnPirateShuttleSplit(Entity<PirateShuttleComponent> ent, ref GridSplitEvent args)
    {
        // If the trade station gets bombed it's still a trade station.
        foreach (var gridUid in args.NewGrids)
        {
            // This *should* be enough to make sure the console doesn't get borked if the shuttle gets wrecked
            EnsureComp<PirateShuttleComponent>(gridUid);
            EnsureComp<StationCargoBountyDatabaseComponent>(gridUid);
            EnsureComp<StationCargoOrderDatabaseComponent>(gridUid);
        }
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
        if (!TryGetPirateShuttle(ent.Owner, out var shuttle))
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
        var allBounties = _protoMan.EnumeratePrototypes<CargoBountyPrototype>()
            .Where(p => p.Group == component.Group)
            .ToList();
        var filteredBounties = new List<CargoBountyPrototype>();
        foreach (var proto in allBounties)
        {
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
        if (!TryGetPirateShuttleComp(ent.Owner, out var shuttle) || shuttle == null)
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
        UpdateOrderState(ent.Owner, shuttle);
    }

    private void OnInteractUsing(Entity<PirateOrderConsoleComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<CashComponent>(args.Used))
            return;

        var price = _pricing.GetPrice(args.Used);

        if (price == 0)
            return;

        if (!TryGetPirateShuttle(ent.Owner, out var shuttle) || shuttle == null)
            return;

        if (!TryGetPirateShuttleComp(ent.Owner, out var shuttleComp) || shuttleComp == null)
            return;

        _audio.PlayPvs(ApproveSound, ent.Owner);
        shuttleComp.Money += price;
        Dirty((EntityUid)shuttle, shuttleComp);
        QueueDel(args.Used);
        args.Handled = true;
    }

    private void OnAddOrderMessage(Entity<PirateOrderConsoleComponent> ent, ref CargoConsoleAddOrderMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (args.Amount <= 0)
            return;

        if (TryGetPirateShuttle(ent.Owner, out var shuttle))
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

        var data = new CargoOrderData(GenerateOrderId(orderDatabase), product.Product, product.Name, product.Cost, args.Amount, args.Requester, args.Reason, cargoOrderConsoleComponent.Account);

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
            return;
        }

        var cost = order.Price * order.OrderQuantity;
        if (!TryGetPirateShuttleComp(ent.Owner, out var shuttleComp) || shuttleComp == null)
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
            FulfillOrder(order, cargoOrderConsoleComponent.Account.Id, ent.Owner.ToCoordinates(), null);

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
                _radio.SendRadioMessage(ent.Owner, message, cargoOrderConsoleComponent.AnnouncementChannel, ent.Owner, escapeMarkup: false);
                if (CargoOrderConsoleComponent.BaseAnnouncementChannel != cargoOrderConsoleComponent.AnnouncementChannel)
                    _radio.SendRadioMessage(ent.Owner, message, CargoOrderConsoleComponent.BaseAnnouncementChannel, ent.Owner, escapeMarkup: false);
        }

        _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-trade-station", ("destination", MetaData(ev.FulfillmentEntity.Value).EntityName)));

        orderDatabase.Orders[cargoOrderConsoleComponent.Account].Remove(order);
        shuttleComp.Money -= cost;
        UpdateOrders(shuttle.Value);
    }

    private bool FulfillOrder(CargoOrderData order, ProtoId<CargoAccountPrototype> account, EntityCoordinates spawn, string? paperProto)
        {
            // Create the item itself
            var item = Spawn(order.ProductId, spawn);

            // Ensure the item doesn't start anchored
            _transform.Unanchor(item, Transform(item));

            // Create a sheet of paper to write the order details on
            var printed = EntityManager.SpawnEntity(paperProto, spawn);
            if (TryComp<PaperComponent>(printed, out var paper))
            {
                // fill in the order data
                var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
                _metaSystem.SetEntityName(printed, val);

                var accountProto = _protoMan.Index(account);
                _paperSystem.SetContent((printed, paper),
                    Loc.GetString(
                        "cargo-console-paper-print-text",
                        ("orderNumber", order.OrderId),
                        ("itemName", MetaData(item).EntityName),
                        ("orderQuantity", order.OrderQuantity),
                        ("requester", order.Requester),
                        ("reason", string.IsNullOrWhiteSpace(order.Reason) ? Loc.GetString("cargo-console-paper-reason-default") : order.Reason),
                        ("account", Loc.GetString(accountProto.Name)),
                        ("accountcode", Loc.GetString(accountProto.Code)),
                        ("approver", string.IsNullOrWhiteSpace(order.Approver) ? Loc.GetString("cargo-console-paper-approver-default") : order.Approver)));

                // attempt to attach the label to the item
                if (TryComp<PaperLabelComponent>(item, out var label))
                {
                    _slots.TryInsert(item, label.LabelSlot, printed, null);
                }
            }

            RaiseLocalEvent(item, new CargoOrderFulfilledEvent()); // Moffstation

            return true;

        }

    private void OnOrderUIOpened(EntityUid uid, PirateOrderConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!TryGetPirateShuttle(uid, out var shuttle))
            return;
        UpdateOrderState(uid, shuttle);
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
        var orderQuery = AllEntityQuery<PirateOrderConsoleComponent>();

        while (orderQuery.MoveNext(out var uid, out var _))
        {
            var shuttle = _transform.GetGrid(uid);
            if (shuttle == null)
                continue;
            if (shuttle != dbUid)
                continue;

            UpdateOrderState(uid, shuttle);
        }
    }

    private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
    {
        // We need an arbitrary unique ID to identify orders, since they may
        // want to be cancelled later.
        return ++orderDB.NumOrdersCreated;
    }

    public void UpdateOrderState(EntityUid uid, EntityUid? shuttle) // Moffstation - made public for pirates
    {
        if (!TryGetOrderDatabase(shuttle, out var orderDatabase))
            return;
        if (!TryComp<CargoOrderConsoleComponent>(uid, out var cargoOrderConsole))
            return;

        if (_uiSystem.HasUi(uid, CargoConsoleUiKey.Orders))
        {
            _uiSystem.SetUiState(uid,
                CargoConsoleUiKey.Orders,
                new CargoConsoleInterfaceState(
                    MetaData(shuttle.Value).EntityName,
                    CargoSystem.GetOutstandingOrderCount(orderDatabase, cargoOrderConsole.Account),
                    orderDatabase.Capacity,
                    GetNetEntity(shuttle.Value),
                    orderDatabase.Orders[cargoOrderConsole.Account],
                    _cargoSystem.GetAvailableProducts((uid, cargoOrderConsole))
                ));
        }
    }

    #endregion

    #region Telepad

    // private void OnTelepadFulfillPirateOrder(ref FulfillPirateOrderEvent args)
    // {
    //     var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
    //     while (query.MoveNext(out var uid, out var tele, out var xform))
    //     {
    //         if (tele.CurrentState != CargoTelepadState.Idle)
    //             continue;
    //
    //         if (!this.IsPowered(uid, EntityManager))
    //             continue;
    //
    //         TryGetPirateShuttle(uid, out var shuttle);
    //         if (shuttle != args.Shuttle.Owner)
    //             continue;
    //
    //         // todo cannot be fucking asked to figure out device linking rn but this shouldn't just default to the first port.
    //         if (!_cargoSystem.TryGetLinkedConsole((uid, tele), out var console) ||
    //             console.Value.Owner != args.OrderConsole.Owner)
    //             continue;
    //
    //         for (var i = 0; i < args.Order.OrderQuantity; i++)
    //         {
    //             tele.CurrentOrders.Add(args.Order);
    //         }
    //         tele.Accumulator = tele.Delay;
    //         args.Handled = true;
    //         args.FulfillmentEntity = uid;
    //         return;
    //     }
    // }

    #endregion

    #region SaleConsole

    private void UpdatePalletConsoleInterface(EntityUid uid)
    {
        if (!TryGetPirateShuttle(uid, out var shuttle) || shuttle == null)
        {
            _uiSystem.SetUiState(uid,
                CargoPalletConsoleUiKey.Sale,
                new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }
        GetPalletGoods((EntityUid)shuttle, out var toSell, out var goods);
        var totalAmount = goods.Sum(t => t.Item3);
        _uiSystem.SetUiState(uid,
            CargoPalletConsoleUiKey.Sale,
            new CargoPalletConsoleInterfaceState((int) totalAmount, toSell.Count, true));
    }

    private void OnPalletUIOpen(Entity<PiratePalletConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdatePalletConsoleInterface(ent.Owner);
    }

    private void GetPalletGoods(EntityUid gridUid, out HashSet<EntityUid> toSell,  out HashSet<(EntityUid, OverrideSellComponent?, double)> goods)
    {
        goods = new HashSet<(EntityUid, OverrideSellComponent?, double)>();
        toSell = new HashSet<EntityUid>();

        foreach (var (palletUid, _, _) in GetCargoPallets(gridUid, BuySellType.Sell))
        {
            // Containers should already get the sell price of their children so can skip those.
            _setEnts.Clear();

            _lookup.GetEntitiesIntersecting(
                palletUid,
                _setEnts,
                LookupFlags.Dynamic | LookupFlags.Sundries);

            foreach (var ent in _setEnts)
            {
                // Dont sell:
                // - anything already being sold
                // - anything anchored (e.g. light fixtures)
                // - anything blacklisted (e.g. players).
                if (toSell.Contains(ent))
                {
                    continue;
                }

                if (_xformQuery.TryGetComponent(ent, out var xform) &&
                    (xform.Anchored || !_cargoSystem.CanSell(ent, xform)))
                {
                    continue;
                }

                if (_blacklistQuery.HasComponent(ent))
                    continue;

                var price = _pricing.GetPrice(ent);
                if (price == 0)
                    continue;
                toSell.Add(ent);
                goods.Add((ent, CompOrNull<OverrideSellComponent>(ent), price));
            }
        }
    }

    private List<(EntityUid Entity, PiratePalletComponent Component, TransformComponent PalletXform)> GetCargoPallets(EntityUid gridUid, BuySellType requestType = BuySellType.All)
    {
        _pads.Clear();

        var query = AllEntityQuery<PiratePalletComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var compXform))
        {
            if (compXform.ParentUid != gridUid ||
                !compXform.Anchored)
            {
                continue;
            }

            if ((requestType & comp.PalletType) == 0)
            {
                continue;
            }

            _pads.Add((uid, comp, compXform));

        }

        return _pads;
    }

    private void OnPalletSale(EntityUid uid, PiratePalletConsoleComponent component, CargoPalletSellMessage args)
    {
        var xform = Transform(uid);

        if (!TryGetPirateShuttle(uid, out var shuttle) ||
            !TryGetPirateShuttleComp(uid, out var shuttleComp))
        {
            return;
        }

        if (shuttle == null || shuttleComp == null)
            return;

        if (xform.GridUid is not { } gridUid)
        {
            _uiSystem.SetUiState(uid,
                CargoPalletConsoleUiKey.Sale,
                new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        if (!_cargoSystem.SellPallets(gridUid, (EntityUid)shuttle, out var goods))
            return;

        foreach (var (_, sellComponent, value) in goods)
        {
            shuttleComp.Money += value;
        }

        Dirty((EntityUid)shuttle, shuttleComp);
        _audio.PlayPvs(ApproveSound, uid);
        UpdatePalletConsoleInterface(uid);
    }

    private void OnPalletAppraise(EntityUid uid, PiratePalletConsoleComponent component, CargoPalletAppraiseMessage args)
    {
        UpdatePalletConsoleInterface(uid);
    }

    #endregion

    #region Shared
    public bool TryGetPirateShuttleComp(EntityUid uid, out PirateShuttleComponent? shuttleComp)
    {
        shuttleComp = null;
        if (!TryGetPirateShuttle(uid, out var shuttle) || shuttle == null)
            return false;
        if (!TryComp<PirateShuttleComponent>(shuttle, out shuttleComp))
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
