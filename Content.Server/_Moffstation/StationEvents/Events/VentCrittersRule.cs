using Content.Server._Moffstation.StationEvents.Components;
using Content.Server.Pinpointer;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Random;

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

    protected override void Added(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {

        // Choose location and make sure it's not null
        component.Location = ChooseLocation();
        if (component.Location is not { } location)
        {
            Log.Warning($"Unable to find a valid location for {args.RuleId}!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // Get the event component so we can format the announcement
        if (TryComp<StationEventComponent>(uid, out var stationEventComp) && stationEventComp.StartAnnouncement != null)
        {
            // Get the nearest beacon
            var mapLocation = _transform.ToMapCoordinates(location);
            var nearestBeacon = _navMap.GetNearestBeaconString(mapLocation, onlyName: true);

            // Get the duration, if its null we'll just use 0
            var duration = stationEventComp.Duration?.Seconds ?? 0;

            // Format the announcement with the time and location, if the string doesn't have them it'll still work fine
            stationEventComp.StartAnnouncement =
                Loc.GetString(stationEventComp.StartAnnouncement,
                    ("location", nearestBeacon),
                    ("time", duration));
        }

        base.Added(uid, component, gameRule, args);
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

        //Spawn in the stuff
        for (var i = 0; i < component.SpawnAttempts; i++)
        {
            foreach (var spawn in EntitySpawnCollection.GetSpawns(component.Entries, RobustRandom))
            {
                Spawn(spawn, location);
            }
        }

        // Spawn a special spawn (guaranteed spawn)
        var specialEntry = RobustRandom.Pick(component.SpecialEntries);
        Spawn(specialEntry.PrototypeId, location);

        // Extra chance to spawn additional entities
        if (component.SpecialEntries.Count != 0)
        {
            foreach (var specialSpawn in EntitySpawnCollection.GetSpawns(component.SpecialEntries, RobustRandom))
            {
                Spawn(specialSpawn, location);
            }
        }
    }

    private EntityCoordinates? ChooseLocation()
    {
        // Get a station
        if (!TryGetRandomStation(out var station))
        {
            return null;
        }
        //Query the possible locations
        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();
        // Filter to things on the same station
        while (locations.MoveNext(out _, out _, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                validLocations.Add(transform.Coordinates);
            }
        }
        // Pick one at random
        if (validLocations.Count != 0)
            return _random.Pick(validLocations);
        return null;
    }
}
