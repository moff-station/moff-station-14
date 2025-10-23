using System.Linq;
using Content.Server._Moffstation.StationEvents.Components;
using Content.Server.GameTicking;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VentCrittersRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime > comp.NextPopup &&
                comp.Location is { } location &&
                _gameTicker.IsGameRuleActive(uid))
            {
                _audio.PlayPvs(comp.VentCreakNoise, location);
                _popup.PopupCoordinates(Loc.GetString("station-event-vent-creatures-vent-warning", ("object", MetaData(location).EntityName)), Transform(location).Coordinates, PopupType.MediumCaution);
                comp.NextPopup = _gameTiming.CurTime + comp.PopupDelay;
            }
        }
    }

    protected override void Added(EntityUid uid, VentCrittersRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        // We are doing this before base.Added() so we can modify the announcement to be what we want
        // Choose location and make sure it's not null
        comp.Location = ChooseLocation();
        if (comp.Location is not { } location)
        {
            Log.Warning($"Unable to find a valid location for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the event component so we can format the announcement
        if (TryComp<StationEventComponent>(uid, out var stationEventComp) && stationEventComp.StartAnnouncement != null)
        {
            // Get the nearest beacon
            var mapLocation = _transform.ToMapCoordinates(Transform(location).Coordinates);
            var nearestBeacon = _navMap.GetNearestBeaconString(mapLocation, onlyName: true);

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

        comp.SpawnAttempts ??= Math.Max(EntityQuery<VentCritterSpawnLocationComponent>().Count(), comp.SpawnAttemptsMin);
    }

    protected override void Ended(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        // Make sure the location is not null
        if (component.Location is not { } location)
        {
            Log.Warning($"Location for gamerule {args.RuleId} was null!");
            return;
        }

        RemCompDeferred<JitteringComponent>(location);

        var coords =  Transform(location).Coordinates;

        //Spawn in the stuff
        for (var i = 0; i < component.SpawnAttempts; i++)
        {
            foreach (var spawn in EntitySpawnCollection.GetSpawns(component.Entries, RobustRandom))
            {
                Spawn(spawn, coords);
            }
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
