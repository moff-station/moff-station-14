using System.Linq;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Clothing.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Power.Components;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.GameTicking.Rules;

/// <summary>
/// The system for the gungame gamemode rule.
/// This is mostly a copy of <see cref="Content.Server.GameTicking.Rules.DeathMatchRuleSystem"/> with some
/// key changes for gun game.
/// </summary>
public sealed class GunGameRuleSystem : GameRuleSystem<GunGameRuleComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly OutfitSystem _outfitSystem = default!;
    [Dependency] private readonly RespawnRuleSystem _respawn = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<GunGameRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var gunGame, out var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var newMind = _mind.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mind.SetUserId(newMind, ev.Player.UserId);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            DebugTools.AssertNotNull(mobMaybe);
            var mob = mobMaybe!.Value;

            _mind.TransferTo(newMind, mob);
            _outfitSystem.SetOutfit(mob, gunGame.Gear);
            EnsureComp<KillTrackerComponent>(mob);
            EnsureComp<GunGameTrackerComponent>(mob);

            gunGame.PlayerInfo.TryAdd(ev.Player.UserId,
                new GunGamePlayerTrackingInfo(
                    ev.Player.UserId,
                    new Queue<ProtoId<StartingGearPrototype>>(gunGame.RewardSpawnsQueue)));
            RefreshPlayerLoadout(gunGame.PlayerInfo[ev.Player.UserId], gunGame);

            _respawn.AddToTracker(ev.Player.UserId, (uid, tracker));

            ev.Handled = true;
            break;
        }
    }

    /// <summary>
    /// Activates when a kill is reported. ev.Entity corresponds to the person who was killed.
    /// </summary>
    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<GunGameRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var gunGame, out var respawnTracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            // Don't want other players picking up somebody's gun
            if (TryComp<GunGameTrackerComponent>(ev.Entity, out var gunGameTracker))
                DeleteCurrentLoadout((ev.Entity, gunGameTracker), gunGame);

            // Force them to respawn so they don't have to wait around.
            // This has to happen before we check if a player killed them, because they might have died
            //     to something other than a player.
            if (TryComp<ActorComponent>(ev.Entity, out var actor))
                _respawn.RespawnPlayer((ev.Entity, actor), (uid, respawnTracker));

            // Make sure the thing that killed them was a player, and that it was a player
            //  other than their own foolish self. Can't suicide your way to victory.
            if (ev.Suicide || ev.Primary is not KillPlayerSource killer)
                continue;

            var killerInfo = gunGame.PlayerInfo[killer.PlayerId];
            // Only allow the player to receive their next weapon after they get enough kills.
            if (++killerInfo.Kills < gunGame.KillsPerWeapon)
                continue;

            killerInfo.Kills = 0;
            ProgressPlayerReward(killerInfo, gunGame);
            RefreshPlayerLoadout(killerInfo, gunGame);
        }
    }

    /// <summary>
    /// Appends round end text at the end of the round.
    /// </summary>
    protected override void AppendRoundEndText(EntityUid uid,
        GunGameRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        if (component.Victor is { } victor && _player.TryGetPlayerData(victor, out var data))
        {
            args.AddLine(Loc.GetString("gun-game-scoreboard-winner", ("player", data.UserName)));
            args.AddLine("");
        }

        args.AddLine(Loc.GetString("gun-game-scoreboard-header"));
        args.AddLine(GetScoreboard((uid, component)).ToMarkup());
    }

    /// <summary>
    /// Deletes a GunGameRewardTrackerComponent's gear.
    /// </summary>
    /// <remarks>
    /// Unequips all the spawned entities on the player before deleting them.
    /// Also removes nearby shell casings to clean things up.
    /// todo: once WeakEntityReference is implemented, ensure it's utilized here.
    /// </remarks>
    private void DeleteCurrentLoadout(Entity<GunGameTrackerComponent> playerEntity, GunGameRuleComponent rule)
    {
        // drop whatever is in their hands
        _hands.TryDrop(playerEntity.Owner, null, false, false);

        // delete all their rewards
        foreach (var entity in playerEntity.Comp.CurrentRewards)
        {
            if (_inventory.TryGetContainingSlot(entity, out var slot))
                _inventory.TryUnequip(playerEntity, playerEntity, slot.Name, true, true, true);

            PredictedQueueDel(entity);
        }

        // delete nearby shell casings
        foreach (var entity in _lookup.GetEntitiesInRange<CartridgeAmmoComponent>(
                     new EntityCoordinates(playerEntity.Owner, 0.0f, 0.0f),
                     rule.CasingDeletionRange))
        {
            if (entity.Comp.Spent && _random.Prob(rule.CasingDeletionProb))
                PredictedQueueDel(entity.Owner);
        }
    }

    /// <summary>
    /// Progresses the specified user's reward queue.
    /// If their queue is empty then they've won so we trigger the round end.
    /// </summary>
    /// <param name="playerInfo">The player's current round info</param>
    /// <param name="rule">The GunGameRuleComponent for this gamerule</param>
    private void ProgressPlayerReward(GunGamePlayerTrackingInfo playerInfo, GunGameRuleComponent rule)
    {
        if (playerInfo.RewardQueue.Count > 1 // We want the last weapon to remain in the queue so we can peek it later.
            && playerInfo.RewardQueue.TryDequeue(out _))
            return;

        rule.Victor = playerInfo.UserId;
        _roundEnd.EndRound(rule.RestartDelay);
    }

    /// <summary>
    /// Spawns the player's current weapon in their queue, intended to be used on spawn and when they upgrade weapons.
    /// </summary>
    /// <param name="playerInfo">The player's current round info</param>
    /// <param name="rule">The GunGameRuleComponent for this gamerule</param>
    private void RefreshPlayerLoadout(GunGamePlayerTrackingInfo playerInfo, GunGameRuleComponent rule)
    {
        if (!_mind.TryGetMind(playerInfo.UserId, out _, out var mind))
            return;

        if (mind.OwnedEntity is not { } playerEntity)
            return;

        var currentGear = EnsureComp<GunGameTrackerComponent>(playerEntity);
        DeleteCurrentLoadout((playerEntity, currentGear), rule);

        if (playerInfo.RewardQueue.Count == 0) // If we somehow try to spawn somebody's weapon after they win
            return;

        // Peek the player's queue, and spawn their items
        SpawnPlayerLoadout((playerEntity, currentGear), playerInfo.RewardQueue.Peek(), rule);
    }

    /// <summary>
    /// Spawns all the items specified in the player's current loadout. Dropping anything in a slot being filled.
    /// This is used instead of one of the pre-existing equipping methods since we need to keep track of what is being
    /// spawned.
    /// </summary>
    private void SpawnPlayerLoadout(Entity<GunGameTrackerComponent> target,
        ProtoId<StartingGearPrototype> gear,
        GunGameRuleComponent gameRule)
    {
        if (!_proto.TryIndex(gear, out var startingGear))
            return;

        foreach (var (slotName, itemProto) in startingGear.Equipment)
        {
            _inventory.TryUnequip(target, slotName, true, true, false);
            var itemEntity = Spawn(itemProto);
            target.Comp.CurrentRewards.Add(itemEntity);
            UpgradeEnergyWeapon(itemEntity, gameRule);
            _inventory.TryEquip(target.Owner, itemEntity, slotName, silent: true, force: true);
        }

        foreach (var itemProto in startingGear.Inhand)
        {
            var itemEntity = Spawn(itemProto);
            target.Comp.CurrentRewards.Add(itemEntity);
            UpgradeEnergyWeapon(itemEntity, gameRule);
            _hands.TryForcePickupAnyHand(target.Owner, itemEntity);
        }
    }

    /// <summary>
    /// Formats the scoreboard for the end of round screen.
    /// </summary>
    /// <returns>
    /// The formatted message to add to the round-end screen.
    /// Returns an empty message if the component doesn't exist.
    /// </returns>
    private FormattedMessage GetScoreboard(Entity<GunGameRuleComponent?> entity)
    {
        var msg = new FormattedMessage();

        if (!Resolve(entity, ref entity.Comp))
            return msg;

        foreach (var (idx, playerInfo) in entity.Comp.PlayerInfo.Values.OrderBy(p => p.RewardQueue.Count).Index())
        {
            if (!_player.TryGetPlayerData(playerInfo.UserId, out var data))
                continue;

            var weaponName = Loc.GetString("gun-game-scoreboard-list-empty");
            if (_proto.TryIndex(playerInfo.RewardQueue.Peek(), out var gear) &&
                _proto.TryIndex(gear.Inhand.First(), out var proto))
                weaponName = proto.Name;

            msg.AddMarkupOrThrow(Loc.GetString("gun-game-scoreboard-list-entry",
                ("place", idx + 1),
                ("name", data.UserName),
                ("weaponsLeft", playerInfo.RewardQueue.Count - 1),
                ("weapon", weaponName)));
            msg.PushNewline();
        }

        return msg;
    }

    /// <summary>
    /// Upgrades an energy weapon to have a rechargeable battery.
    /// This does account for whether the weapon contains a battery itself,
    /// at which point it grabs that battery and upgrades it instead.
    /// Also, this will set the recharge rate to only increase up to
    /// <see cref="GunGameRuleComponent.DefaultEnergyWeaponRechargeRate"/>.
    /// If the weapon already has a recharge rate higher than this, it uses that instead.
    /// </summary>
    private void UpgradeEnergyWeapon(EntityUid uid, GunGameRuleComponent rule )
    {
        var upgradeEnt = HasComp<BatteryComponent>(uid)
            ? uid
            : TryComp<PowerCellSlotComponent>(uid, out var cellSlot)
                ? _itemSlots.GetItemOrNull(uid, cellSlot.CellSlotId)
                : null;

        if (upgradeEnt is not { } upgradable)
            return;

        var batteryRecharge = EnsureComp<BatterySelfRechargerComponent>(upgradable);
        batteryRecharge.AutoRecharge = true;
        batteryRecharge.AutoRechargeRate = Math.Max(rule.DefaultEnergyWeaponRechargeRate, batteryRecharge.AutoRechargeRate);
    }
}
