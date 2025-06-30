using System.Linq;
using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.Clothing.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Network;
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
    [Dependency] private readonly EntityTableSystem _entityTables = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
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
            SpawnCurrentWeapon(ev.Player.UserId, gunGame);
            _respawn.AddToTracker(ev.Player.UserId, (uid, tracker));

            ev.Handled = true;
            break;
        }
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<KillTrackerComponent>(ev.Mob);
        EnsureComp<GunGameTrackerComponent>(ev.Mob);
        var query = EntityQueryEnumerator<GunGameRuleComponent, RespawnTrackerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var tracker, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;
            _respawn.AddToTracker((ev.Mob, null), (uid, tracker));
        }
    }

    /// <summary>
    /// Activates when a kill is reported. ev.Entity corresponds to the person who was killed.
    /// </summary>
    private void OnKillReported(ref KillReportedEvent ev)
    {
        // Don't want other players picking up somebody's gun
        if (TryComp<GunGameTrackerComponent>(ev.Entity, out var tracker))
            DeleteCurrentWeapons(tracker);

        var query = EntityQueryEnumerator<GunGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var gunGame, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule)
                || ev.Primary is not KillPlayerSource player)
                continue;

            // Only allow the player to receive their next weapon after they get enough kills
            gunGame.PlayerKills.TryAdd(player.PlayerId, 0);
            if (++gunGame.PlayerKills[player.PlayerId] < gunGame.KillsPerWeapon)
                continue;

            gunGame.PlayerKills[player.PlayerId] = 0;
            ProgressPlayerReward(player.PlayerId, gunGame);
            SpawnCurrentWeapon(player.PlayerId, gunGame);
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
    /// <param name="gear">The component with the gear.</param>
    /// <remarks>
    /// Right now this is mostly just a foreach loop which deletes all the entities in the list.
    /// This does trigger an exception in the client if a gun is deleting while it's bullets are still active in the world.
    /// todo: once WeakEntityReference is implemented, ensure it's utilized here.
    /// </remarks>
    private void DeleteCurrentWeapons(GunGameTrackerComponent gear)
    {
        foreach (var entity in gear.CurrentRewards)
        {
            QueueDel(entity);
        }
    }

    /// <summary>
    /// Progresses the specified user's reward queue.
    /// If their queue is empty then they've won so we trigger the round end.
    /// </summary>
    /// <param name="userId">The player's user ID</param>
    /// <param name="rule">The GunGameRuleComponent for this gamerule</param>
    private void ProgressPlayerReward(NetUserId userId, GunGameRuleComponent rule)
    {
        if (!rule.PlayerRewards.TryGetValue(userId, out var rewardQueue))
            return;

        if (rewardQueue.TryDequeue(out _))
            return;

        rule.Victor = userId;
        _roundEnd.EndRound(rule.RestartDelay);
    }

    /// <summary>
    /// Spawns the player's current weapon in their queue, intended to be used on spawn and when they upgrade weapons.
    /// </summary>
    /// <param name="userId">The player's user ID</param>
    /// <param name="rule">The GunGameRuleComponent for this gamerule</param>
    private void SpawnCurrentWeapon(NetUserId userId, GunGameRuleComponent rule)
    {
        if (!_mind.TryGetMind(userId, out var _, out var mind))
            return;

        if (mind.OwnedEntity is not { } playerEntity)
            return;

        var gear = EnsureComp<GunGameTrackerComponent>(playerEntity);
        DeleteCurrentWeapons(gear);

        if (!rule.PlayerRewards.TryGetValue(userId, out var gearQueue))
        {
            gearQueue = new Queue<EntityTableSelector>(rule.RewardSpawnsQueue);
            rule.PlayerRewards.Add(userId, gearQueue);
        }

        if (gearQueue.Count == 0) // If we somehow try to spawn somebody's weapon after they win
            return;

        var slotTryOrder = new Queue<string>(rule.SlotTryOrder);
        // Peek the player's queue, and spawn their items
        foreach (var item in _entityTables.GetSpawns(gearQueue.Peek()))
        {
            SpawnItemAndEquip(
                (playerEntity, gear),
                item,
                slotTryOrder);
        }
    }

    /// <summary>
    /// Attempts to spawn an item on a player in either their hands or the next available equipment slot according to
    /// <see cref="Components.GunGameRuleComponent.SlotTryOrder"/>.
    /// </summary>
    private void SpawnItemAndEquip(Entity<GunGameTrackerComponent> player,
        EntProtoId itemProto,
        Queue<string> slotTryOrder)
    {
        var itemEnt = Spawn(itemProto);
        player.Comp.CurrentRewards.Add(itemEnt);
        while (slotTryOrder.TryDequeue(out var slot))
        {
            if (slot == "hand" && _hands.TryForcePickupAnyHand(player, itemEnt)
                || _inventory.TryEquip(player, player, itemEnt, slot, true, true))
                return;
        }

        // if there's no slots that work, drop it on the ground
        _transform.SetCoordinates(itemEnt,
            new EntityCoordinates(player,
                _random.NextFloat() - 0.5f,
                _random.NextFloat() - 0.5f));
        _transform.SetLocalRotation(itemEnt, _random.NextAngle());
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

        foreach (var (idx, (id, playerQueue)) in entity.Comp.PlayerRewards.OrderBy(p => p.Value.Count).Index())
        {
            if (!_player.TryGetPlayerData(id, out var data))
                continue;

            msg.AddMarkupOrThrow(Loc.GetString("gun-game-scoreboard-list-entry",
                ("place", idx + 1),
                ("name", data.UserName),
                ("weaponsLeft", playerQueue.Count),
                ("weapon",
                    !playerQueue.TryPeek(out var itemList)
                       || !_proto.TryIndex(
                           _entityTables.GetSpawns(itemList)
                               .First(), // We assume the first item in the entity table is the player's weapon.
                           out var proto)
                    ? "None"
                    : proto.Name)));
            msg.PushNewline();
        }

        return msg;
    }
}
