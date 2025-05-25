using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._Moffstation.Pirate.Components;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Moffstation.Cargo.Components;
using Content.Shared._Moffstation.Cargo.Events;
using Content.Shared._Moffstation.Pirate.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
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
using Content.Shared.Station.Components;
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
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<CargoSellBlacklistComponent> _blacklistQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<TradeStationComponent> _tradeQuery;

    // Should always keep this disabled for pirates, unless we find another use for it
    private bool _lockboxCutEnabled = false;

    private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg");

    private HashSet<EntityUid> _setEnts = new();
    private List<EntityUid> _listEnts = new();
    private List<(EntityUid, CargoPalletComponent, TransformComponent)> _pads = new();

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _blacklistQuery = GetEntityQuery<CargoSellBlacklistComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _tradeQuery = GetEntityQuery<TradeStationComponent>();

        // Kinda order console
        // SubscribeLocalEvent<PirateOrderConsoleComponent, CargoConsoleWithdrawFundsMessage>(OnWithdrawFunds);

        // Order console
        SubscribeLocalEvent<PirateOrderConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);

        //Sale Console
        SubscribeLocalEvent<PiratePalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);
        SubscribeLocalEvent<PiratePalletConsoleComponent, CargoPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<PiratePalletConsoleComponent, CargoPalletAppraiseMessage>(OnPalletAppraise);
    }

    #region OrderConsole

    private void OnApproveOrderMessage(Entity<PirateOrderConsoleComponent> ent, ref CargoConsoleApproveOrderMessage args)
    {
        if (!TryComp<CargoOrderConsoleComponent>(ent.Owner, out var component))
            return;

        if (args.Actor is not { Valid: true } player)
            return;

        if (component.SlipPrinter)
            return;

        if (!_accessReaderSystem.IsAllowed(player, ent.Owner))
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
            _cargoSystem.PlayDenySound(ent.Owner, component);
            return;
        }

        var station = _station.GetOwningStation(ent.Owner);

        // No station to deduct from.
        if (!TryComp(station, out StationBankAccountComponent? bank) ||
            !TryComp(station, out StationDataComponent? stationData) ||
            !_cargoSystem.TryGetOrderDatabase(station, out var orderDatabase))
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-station-not-found"));
            _cargoSystem.PlayDenySound(ent.Owner, component);
            return;
        }

        var orderId = args.OrderId;
        // Find our order again. It might have been dispatched or approved already
        var order = orderDatabase.Orders[component.Account].Find(order => orderId == order.OrderId && !order.Approved);
        if (order == null || !_protoMan.TryIndex(order.Account, out var account))
        {
            return;
        }

        // Invalid order
        if (!_protoMan.HasIndex<EntityPrototype>(order.ProductId))
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-invalid-product"));
            _cargoSystem.PlayDenySound(ent.Owner, component);
            return;
        }

        var amount = CargoSystem.GetOutstandingOrderCount(orderDatabase, order.Account);
        var capacity = orderDatabase.Capacity;

        // Too many orders, avoid them getting spammed in the UI.
        if (amount >= capacity)
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-too-many"));
            _cargoSystem.PlayDenySound(ent.Owner, component);
            return;
        }

        // Cap orders so someone can't spam thousands.
        var cappedAmount = Math.Min(capacity - amount, order.OrderQuantity);

        if (cappedAmount != order.OrderQuantity)
        {
            order.OrderQuantity = cappedAmount;
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-snip-snip"));
            _cargoSystem.PlayDenySound(ent.Owner, component);
        }

        var cost = order.Price * order.OrderQuantity;
        var accountBalance = _cargoSystem.GetBalanceFromAccount((station.Value, bank), order.Account);

        // Not enough balance
        if (cost > accountBalance)
        {
            _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-insufficient-funds", ("cost", cost)));
            _cargoSystem.PlayDenySound(ent.Owner, component);
            return;
        }

        var ev = new FulfillCargoOrderEvent((station.Value, stationData), order, (ent.Owner, component));
        RaiseLocalEvent(ref ev);
        ev.FulfillmentEntity ??= station.Value;

        if (!ev.Handled)
        {
            ev.FulfillmentEntity = TryFulfillOrder((station.Value, stationData), order.Account, order, orderDatabase);

            if (ev.FulfillmentEntity == null)
            {
                _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-unfulfilled"));
                _cargoSystem.PlayDenySound(ent.Owner, component);
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
            _radio.SendRadioMessage(ent.Owner, message, account.RadioChannel, ent.Owner, escapeMarkup: false);
            if (CargoOrderConsoleComponent.BaseAnnouncementChannel != account.RadioChannel)
                _radio.SendRadioMessage(ent.Owner, message, CargoOrderConsoleComponent.BaseAnnouncementChannel, ent.Owner, escapeMarkup: false);
        }

        _cargoSystem.ConsolePopup(args.Actor, Loc.GetString("cargo-console-trade-station", ("destination", MetaData(ev.FulfillmentEntity.Value).EntityName)));

        // Log order approval
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(player):user} approved order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}] on account {order.Account} with balance at {accountBalance}");

        orderDatabase.Orders[component.Account].Remove(order);
        _cargoSystem.UpdateBankAccount((station.Value, bank), -cost, order.Account);
        _cargoSystem.UpdateOrders(station.Value);
    }

    private EntityUid? TryFulfillOrder(Entity<StationDataComponent> stationData, ProtoId<CargoAccountPrototype> account, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase)
    {
        EntityUid? tradeDestination = null;

        // Try to fulfill from any station where possible, if the pad is not occupied.

        var tradePads = GetCargoPallets(stationData.Owner, BuySellType.Buy);
        _random.Shuffle(tradePads);

        var freePads = _cargoSystem.GetFreeCargoPallets(stationData.Owner, tradePads);
        if (freePads.Count >= order.OrderQuantity) //check if the station has enough free pallets
        {
            foreach (var pad in freePads)
            {
                var coordinates = new EntityCoordinates(stationData.Owner, pad.Transform.LocalPosition);

                if (_cargoSystem.FulfillOrder(order, account, coordinates, orderDatabase.PrinterOutput))
                {
                    tradeDestination = stationData.Owner;
                    order.NumDispatched++;
                    if (order.OrderQuantity <= order.NumDispatched) //Spawn a crate on free pellets until the order is fulfilled.
                        break;
                }
            }
        }

        return tradeDestination;
    }

    #endregion

    #region SaleConsole

    private void UpdatePalletConsoleInterface(EntityUid uid)
    {
        if (Transform(uid).GridUid is not { } gridUid)
        {
            _uiSystem.SetUiState(uid,
                CargoPalletConsoleUiKey.Sale,
                new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }
        GetPalletGoods(gridUid, out var toSell, out var goods);
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

    private List<(EntityUid Entity, CargoPalletComponent Component, TransformComponent PalletXform)> GetCargoPallets(EntityUid gridUid, BuySellType requestType = BuySellType.All)
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

            if (TryComp<CargoPalletComponent>(uid, out var cargoComp))

            _pads.Add((uid, cargoComp, compXform));

        }

        return _pads;
    }

    private void OnPalletSale(EntityUid uid, PiratePalletConsoleComponent component, CargoPalletSellMessage args)
    {
        var xform = Transform(uid);

        if (_station.GetOwningStation(uid) is not { } station ||
            !TryComp<StationBankAccountComponent>(station, out var bankAccount))
        {
            return;
        }

        if (xform.GridUid is not { } gridUid)
        {
            _uiSystem.SetUiState(uid,
                CargoPalletConsoleUiKey.Sale,
                new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        if (!SellPallets(gridUid, station, out var goods))
            return;

        var baseDistribution = _cargoSystem.CreateAccountDistribution((station, bankAccount));
        foreach (var (_, sellComponent, value) in goods)
        {
            Dictionary<ProtoId<CargoAccountPrototype>, double> distribution;
            if (sellComponent != null)
            {
                var cut = _lockboxCutEnabled ? bankAccount.LockboxCut : bankAccount.PrimaryCut;
                distribution = new Dictionary<ProtoId<CargoAccountPrototype>, double>
                {
                    { sellComponent.OverrideAccount, cut },
                    { bankAccount.PrimaryAccount, 1.0 - cut },
                };
            }
            else
            {
                distribution = baseDistribution;
            }

            _cargoSystem.UpdateBankAccount((station, bankAccount), (int) Math.Round(value), distribution, false);
        }

        Dirty(station, bankAccount);
        _audio.PlayPvs(ApproveSound, uid);
        UpdatePalletConsoleInterface(uid);
    }

    internal bool SellPallets(EntityUid gridUid, EntityUid station, out HashSet<(EntityUid, OverrideSellComponent?, double)> goods)
    {
        GetPalletGoods(gridUid, out var toSell, out goods);

        if (toSell.Count == 0)
            return false;

        var ev = new EntitySoldEvent(toSell, station);
        RaiseLocalEvent(ref ev);

        foreach (var ent in toSell)
        {
            Del(ent);
        }

        return true;
    }

    private void OnPalletAppraise(EntityUid uid, PiratePalletConsoleComponent component, CargoPalletAppraiseMessage args)
    {
        UpdatePalletConsoleInterface(uid);
    }

    #endregion
}
