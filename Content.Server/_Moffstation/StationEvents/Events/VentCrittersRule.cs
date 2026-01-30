using System.Linq;
using Content.Server._Moffstation.StationEvents.Components;
using Content.Server.GameTicking;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.StationEvents.Events;

public sealed class VentCrittersRule : StationEventSystem<VentCrittersRuleComponent>
{
    /*
     * DO NOT COPY PASTE THIS TO MAKE YOUR MOB EVENT.
     * USE THE PROTOTYPE.
     */

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VentCrittersRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime <= comp.NextPopup ||
                comp.Coords is not { } coords ||
                !_gameTicker.IsGameRuleActive(uid))
                continue;

            _audio.PlayPvs(comp.VentCreakNoise, coords);

            var messageString = comp.Vent is not { } location || !_entMan.EntityExists(location)
                ? Loc.GetString("station-event-vent-creatures-no-vent-warning")
                : Loc.GetString("station-event-vent-creatures-vent-warning", ("object", MetaData(location).EntityName));

            _popup.PopupCoordinates(messageString, coords, PopupType.MediumCaution);
            comp.NextPopup = _gameTiming.CurTime + comp.PopupDelay;
        }
    }

    protected override void Added(EntityUid uid, VentCrittersRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        // We are doing this before base.Added() so we can modify the announcement to be what we want
        // Choose location and make sure it's not null
        comp.Vent = ChooseLocation();
        if (comp.Vent is not { } location)
        {
            Log.Warning($"Unable to find a valid location for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the event component so we can format the announcement
        if (TryComp<StationEventComponent>(uid, out var stationEventComp) && stationEventComp.StartAnnouncement != null)
        {
            // Get the nearest beacon
            var coords = Transform(location).Coordinates;
            comp.Coords = coords;
            var nearestBeacon = _navMap.GetNearestBeaconString(_transform.ToMapCoordinates(coords), onlyName: true);

            // Get the duration, if its null we'll just use 0
            var duration = stationEventComp.Duration?.Seconds ?? 0;

            // Format the announcement with the time and location, if the string doesn't have them it'll still work fine
            stationEventComp.StartAnnouncement =
                Loc.GetString(stationEventComp.StartAnnouncement,
                    ("location", nearestBeacon),
                    ("time", duration));
        }

        base.Added(uid, comp, gameRule, args);

        _jitter.AddJitter(location, 0.5f, 30f);

        // Get spawn attempts
        var playerCount = _playerManager.Sessions.Count(x =>
            GameTicker.PlayerGameStatuses.TryGetValue(x.UserId, out var status) &&
            status == PlayerGameStatus.JoinedGame);
        comp.SpawnAttempts = _random.Next(playerCount * comp.PlayerRatioSpawnsMin, playerCount * comp.PlayerRatioSpawnsMax);
    }

    protected override void Ended(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        // Make sure the location is not null
        if (component.Vent is { } vent)
            RemCompDeferred<JitteringComponent>(vent);

        if (component.Coords is not { } coords)
        {
            Log.Warning($"Unable to find a valid location for {args.RuleId}!");
            return;
        }

        var spawnCount = 0;
        var attemptCount = 0;
        while (spawnCount <= component.MaxSpawns && (attemptCount < component.SpawnAttempts || spawnCount == 0))
        {
            var spawned = EntityManager.SpawnEntitiesAttachedTo(
                coords,
                EntitySpawnCollection.GetSpawns(component.Entries, RobustRandom).Select(it => (EntProtoId)it)
            );
            spawnCount += spawned.Length;
            attemptCount += 1;
        }

        // Extra chance to spawn additional entities
        if (component.SpecialEntries.Count != 0)
        {
            // Spawn a special spawn (guaranteed spawn)
            var specialEntry = RobustRandom.Pick(component.SpecialEntries);
            Spawn(specialEntry.PrototypeId, coords);

            foreach (var specialSpawn in EntitySpawnCollection.GetSpawns(component.SpecialEntries, RobustRandom))
            {
                Spawn(specialSpawn, coords);
            }
        }
    }

    private EntityUid? ChooseLocation()
    {
        // Get a station
        if (!TryGetRandomStation(out var station))
        {
            return null;
        }
        //Query the possible locations
        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityUid>();
        // Filter to things on the same station
        while (locations.MoveNext(out var uid, out var comp, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                validLocations.Add(uid);
            }
        }
        // Pick one at random
        if (validLocations.Count != 0)
            return _random.Pick(validLocations);
        return null;
    }
}
