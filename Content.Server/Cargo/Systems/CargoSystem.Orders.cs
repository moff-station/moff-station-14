using System.Diagnostics.CodeAnalysis;
using Content.Server._Moffstation.Cargo.Events; // Moffstation - Cargo Server
using Content.Server.Cargo.Components;
using Content.Server.Station.Components;
using Content.Shared._Moffstation.Cargo.Events; // Moffstation
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems
{
    public sealed partial class CargoSystem
    {
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly EmagSystem _emag = default!;

        private void InitializeConsole()
        {
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleAddOrderMessage>(OnAddOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleRemoveOrderMessage>(OnRemoveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleApproveOrderMessage>(OnApproveOrderMessage);
            SubscribeLocalEvent<CargoOrderConsoleComponent, BoundUIOpenedEvent>(OnOrderUIOpened);
            SubscribeLocalEvent<CargoOrderConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CargoOrderConsoleComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<CargoOrderConsoleComponent, GotEmaggedEvent>(OnEmagged);
        }

        private void OnInteractUsing(EntityUid uid, CargoOrderConsoleComponent component, ref InteractUsingEvent args)
        {
            if (!HasComp<CashComponent>(args.Used))
                return;

            // Moffstation - Start - Cargo Server
            if (GetLinkedCargoServer(uid) is not { } server)
            {
                // No linked server to add money to.
                return;
            }
            // Moffstation - End

            var price = _pricing.GetPrice(args.Used);

            if (price == 0)
                return;

            // Moffstation - removed station bank account initialization here.

            _audio.PlayPvs(ApproveSound, uid);
            UpdateBankAccount((server, server.Comp1), (int) price, CreateAccountDistribution(component.Account, server)); // Moffstation - Cargo Server
            QueueDel(args.Used);
            args.Handled = true;
        }

        private void OnInit(EntityUid uid, CargoOrderConsoleComponent orderConsole, ComponentInit args)
        {
            UpdateOrderState((uid, orderConsole)); // Moffstation - Cargo Server
        }

        private void OnEmagged(Entity<CargoOrderConsoleComponent> ent, ref GotEmaggedEvent args)
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(ent, EmagType.Interaction))
                return;

            args.Handled = true;
        }

        private void UpdateConsole()
        {
            var stationQuery = EntityQueryEnumerator<StationBankAccountComponent>();
            while (stationQuery.MoveNext(out var uid, out var bank))
            {
                if (Timing.CurTime < bank.NextIncomeTime)
                    continue;
                bank.NextIncomeTime += bank.IncomeDelay;

                var balanceToAdd = (int) Math.Round(bank.IncreasePerSecond * bank.IncomeDelay.TotalSeconds);
                UpdateBankAccount((uid, bank), balanceToAdd, bank.RevenueDistribution);
            }
        }

        #region Interface

        private void OnApproveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleApproveOrderMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (!_accessReaderSystem.IsAllowed(player, uid))
            {
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-order-not-allowed"));
                PlayDenySound(uid, component);
                return;
            }

            // Moffstation - Start - Cargo Server
            if (GetLinkedCargoServer(uid) is not { } server)
            {
                // TODO CENT Loc here is wrong
                ConsolePopup(args.Actor, Loc.GetString("cargo-console-station-not-found"));
                // Moffstation - End
                PlayDenySound(uid, component);
                return;
            }

            // Moffstation - Start - Cargo Server
            //   All of the order fulfillment logic used to be here, but now we send a message to the server and the
            //   server handles the fulfillment.
            var ev = new CargoApprovedOrderMessage(
                args.OrderId,
                component.Account,
                shouldAnnounceFulfillment: !_emag.CheckFlag(uid, EmagType.Interaction),
                    uid,
                    player,
                // TODO CENT The channel can be resolved from the account.
                    component.AnnouncementChannel
            );
            RaiseLocalEvent(server, ev);

            if (ev.DenialReason is { } denialReason)
            {
                ConsolePopup(args.Actor, denialReason);
                PlayDenySound(uid, component);
                return;
            }

            _audio.PlayPvs(ApproveSound, uid);
            // Moffstation - End
        }

        private EntityUid? TryFulfillOrder(Entity<StationDataComponent> stationData, CargoOrderData order, StationCargoOrderDatabaseComponent orderDatabase) // Moffstation - Cargo Server
        {
            // No slots at the trade station
            // Moffstation - Start - Replace static entity list with local
            List<EntityUid> listEnts = [];
            GetTradeStations(stationData, ref listEnts);
            // Moffstation - End
            EntityUid? tradeDestination = null;

            // Try to fulfill from any station where possible, if the pad is not occupied.
            foreach (var trade in listEnts) // Moffstation - Local entity list
            {
                var tradePads = GetCargoPallets(trade, BuySellType.Buy);
                _random.Shuffle(tradePads);

                var freePads = GetFreeCargoPallets(trade, tradePads);
                if (freePads.Count >= order.OrderQuantity) //check if the station has enough free pallets
                {
                    foreach (var pad in freePads)
                    {
                        var coordinates = new EntityCoordinates(trade, pad.Transform.LocalPosition);

                        if (FulfillOrder(order, coordinates, orderDatabase.PrinterOutput)) // Moffstation - Cargo Server
                        {
                            tradeDestination = trade;
                            order.NumDispatched++;
                            if (order.OrderQuantity <= order.NumDispatched) //Spawn a crate on free pellets until the order is fulfilled.
                                break;
                        }
                    }
                }

                if (tradeDestination != null)
                    break;
            }

            return tradeDestination;
        }

        private void GetTradeStations(StationDataComponent data, ref List<EntityUid> ents)
        {
            foreach (var gridUid in data.Grids)
            {
                if (!_tradeQuery.HasComponent(gridUid))
                    continue;

                ents.Add(gridUid);
            }
        }

        private void OnRemoveOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleRemoveOrderMessage args)
        {
            if (GetLinkedCargoServer(uid) is not { } server) // Moffstation - Cargo Server
                return;

            RemoveOrder(component.Account, args.OrderId, server); // Moffstation - Cargo Server
        }

        private void OnAddOrderMessage(EntityUid uid, CargoOrderConsoleComponent component, CargoConsoleAddOrderMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            if (args.Amount <= 0)
                return;

            // Moffstation - Cargo server -- Removed station lookup by the console, that's handled by the cargo server now.

            if (!_protoMan.TryIndex<CargoProductPrototype>(args.CargoProductId, out var product))
            {
                Log.Error($"Tried to add invalid cargo product {args.CargoProductId} as order!");
                return;
            }

            if (!component.AllowedGroups.Contains(product.Group))
                return;

            // Moffstation - Start - Cargo Server
            if (GetLinkedCargoServer(uid) is not { } server)
                return;

            var data = GetOrderData(args, product, component.Account, GenerateOrderId(server));
            // Moffstation - End

            if (!TryAddOrder(data, server)) // Moffstation - Cargo Server
            {
                PlayDenySound(uid, component);
                return;
            }

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(player):user} added order [orderId:{data.OrderId}, quantity:{data.OrderQuantity}, product:{data.ProductId}, requester:{data.Requester}, reason:{data.Reason}]");

        }

        private void OnOrderUIOpened(EntityUid uid, CargoOrderConsoleComponent component, BoundUIOpenedEvent args)
        {
            UpdateOrderState((uid, component)); // Moffstation - Cargo Server
        }

        #endregion

        // Moffstation - CargoServer -- Console know their attached servers, so the station isn't passed in.
        private void UpdateOrderState(Entity<CargoOrderConsoleComponent> entity)
        {
            if (GetLinkedCargoServer(entity) is not { } server ||
                !_uiSystem.HasUi(entity, CargoConsoleUiKey.Orders))
            {
                _uiSystem.SetUiState(
                    entity.Owner,
                    CargoConsoleUiKey.Orders,
                    null
                );
                return;
            }

            if (_station.GetOwningStation(server) is not { } station)
                return;

            _uiSystem.SetUiState(
                entity.Owner,
                CargoConsoleUiKey.Orders,
                new CargoConsoleInterfaceState(
                    MetaData(station).EntityName,
                    GetOutstandingOrderCount(server.Comp2, entity.Comp.Account),
                    server.Comp2.Capacity,
                    GetNetEntity(server),
                    server.Comp2.Orders[entity.Comp.Account]
                )
            );
        }
        // Moffstation - End

        private void ConsolePopup(EntityUid actor, string text)
        {
            _popup.PopupCursor(text, actor);
        }

        private void PlayDenySound(EntityUid uid, CargoOrderConsoleComponent component)
        {
            _audio.PlayPvs(_audio.ResolveSound(component.ErrorSound), uid);
        }

        private static CargoOrderData GetOrderData(CargoConsoleAddOrderMessage args, CargoProductPrototype cargoProduct, ProtoId<CargoAccountPrototype> account, int id) // Moffstation - Cargo Server
        {
            return new CargoOrderData(id, cargoProduct.Product, cargoProduct.Name, cargoProduct.Cost, args.Amount, args.Requester, args.Reason, account); // Moffstation - Cargo Server
        }

        public static int GetOutstandingOrderCount(StationCargoOrderDatabaseComponent component, ProtoId<CargoAccountPrototype> account)
        {
            var amount = 0;

            foreach (var order in component.Orders[account])
            {
                if (!order.Approved)
                    continue;
                amount += order.OrderQuantity - order.NumDispatched;
            }

            return amount;
        }

        /// <summary>
        /// Updates all of the cargo-related consoles for a particular station.
        /// This should be called whenever orders change.
        /// </summary>
        private void UpdateOrders() // Moffstation - Cargo Server
        {
            // Order added so all consoles need updating.
            var orderQuery = AllEntityQuery<CargoOrderConsoleComponent>();

            while (orderQuery.MoveNext(out var uid, out var component)) // Moffstation - Cargo Server
            {
                UpdateOrderState((uid, component)); // Moffstation - Cargo Server
            }

            // Moffstation - Start -- I just obliterated this because it doesn't seem to be used and was getting in the way
            // var consoleQuery = AllEntityQuery<CargoShuttleConsoleComponent>();
            // while (consoleQuery.MoveNext(out var uid, out var _))
            // {
            //     var station = _station.GetOwningStation(uid);
            //     if (station != dbUid)
            //         continue;
            //
            //     UpdateShuttleState(uid, station);
            // }
            // Moffstation - End
        }

        public bool AddAndApproveOrder(
            EntityUid dbUid,
            string spawnId,
            string name,
            int cost,
            int qty,
            string sender,
            string description,
            string dest,
            StationCargoOrderDatabaseComponent component,
            ProtoId<CargoAccountPrototype> account,
            Entity<StationDataComponent> stationData
        )
        {
            DebugTools.Assert(_protoMan.HasIndex<EntityPrototype>(spawnId));
            // Make an order
            var id = GenerateOrderId(component);
            var order = new CargoOrderData(id, spawnId, name, cost, qty, sender, description, account); // Moffstation - Cargo Server

            // Approve it now
            order.SetApproverData(dest, sender);
            order.Approved = true;

            // Log order addition
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"AddAndApproveOrder {description} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}]");

            // Add it to the list
            return TryAddOrder(order, component) && TryFulfillOrder(stationData, order, component).HasValue; // Moffstation - Cargo Server
        }

        // Moffstation - Start - Cargo Server
        private bool TryAddOrder(CargoOrderData data, StationCargoOrderDatabaseComponent component)
        {
            component.Orders[data.Account].Add(data);
            UpdateOrders();
            return true;
        }
        // Moffstation - End

        private static int GenerateOrderId(StationCargoOrderDatabaseComponent orderDB)
        {
            // We need an arbitrary unique ID to identify orders, since they may
            // want to be cancelled later.
            return ++orderDB.NumOrdersCreated;
        }

        public void RemoveOrder(ProtoId<CargoAccountPrototype> account, int index, StationCargoOrderDatabaseComponent orderDB) // Moffstation - Cargo Server
        {
            var sequenceIdx = orderDB.Orders[account].FindIndex(order => order.OrderId == index);
            if (sequenceIdx != -1)
            {
                orderDB.Orders[account].RemoveAt(sequenceIdx);
            }
            UpdateOrders(); // Moffstation - Cargo Server
        }

        public void ClearOrders(StationCargoOrderDatabaseComponent component)
        {
            if (component.Orders.Count == 0)
                return;

            component.Orders.Clear();
        }

        private static bool PopFrontOrder(StationCargoOrderDatabaseComponent orderDB, ProtoId<CargoAccountPrototype> account, [NotNullWhen(true)] out CargoOrderData? orderOut)
        {
            var orderIdx = orderDB.Orders[account].FindIndex(order => order.Approved);
            if (orderIdx == -1)
            {
                orderOut = null;
                return false;
            }

            orderOut = orderDB.Orders[account][orderIdx];
            orderOut.NumDispatched++;

            if (orderOut.NumDispatched >= orderOut.OrderQuantity)
            {
                // Order is complete. Remove from the queue.
                orderDB.Orders[account].RemoveAt(orderIdx);
            }
            return true;
        }

        /// <summary>
        /// Tries to fulfill the next outstanding order.
        /// </summary>
        [PublicAPI]
        private bool FulfillNextOrder(StationCargoOrderDatabaseComponent orderDB, ProtoId<CargoAccountPrototype> account, EntityCoordinates spawn, string? paperProto)
        {
            if (!PopFrontOrder(orderDB, account, out var order))
                return false;

            return FulfillOrder(order, spawn, paperProto); // Moffstation - Cargo Server
        }

        /// <summary>
        /// Fulfills the specified cargo order and spawns paper attached to it.
        /// </summary>
        private bool FulfillOrder(CargoOrderData order, EntityCoordinates spawn, string? paperProto) // Moffstation - Cargo Server
        {
            // Create the item itself
            var item = Spawn(order.ProductId, spawn);

            // Ensure the item doesn't start anchored
            _transformSystem.Unanchor(item, Transform(item));

            // Create a sheet of paper to write the order details on
            var printed = EntityManager.SpawnEntity(paperProto, spawn);
            if (TryComp<PaperComponent>(printed, out var paper))
            {
                // fill in the order data
                var val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", order.OrderId));
                _metaSystem.SetEntityName(printed, val);

                var accountProto = _protoMan.Index(order.Account); // Moffstation - Cargo Server
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

        // Moffstation - Start - Cargo Server
        // #region Station
        //
        // private bool TryGetOrderDatabase([NotNullWhen(true)] EntityUid? stationUid, [MaybeNullWhen(false)] out StationCargoOrderDatabaseComponent dbComp)
        // {
        //     return TryComp(stationUid, out dbComp);
        // }
        //
        // #endregion
        // Moffstation - End
    }
}
