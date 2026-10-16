using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
// Moffstation - Start - Syndicate dead drop
using Content.Server.Radio.EntitySystems;
using Content.Server.Pinpointer;
using Robust.Shared.Utility;
// Moffstation - End

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn a given entity at a random tile on the affected station.
/// </summary>
/// <seealso cref="RandomSpawnRuleComponent"/>
public sealed partial class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    // Moffstation - Start - Syndicate dead drop
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private RadioSystem _radio = default!;
    // Moffstation - End

    protected override void Started(Entity<RandomSpawnRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (Station.TryFindRandomTile(out _, out _, out _, out var coords))
        {
            Sawmill.Info($"Spawning {ent.Comp1.Prototype} at {coords}");
            // Moffstation - Syndicate dead drop
            var spawn = Spawn(ent.Comp1.Prototype, coords);

            if (ent.Comp1.RadioMessage is {} radioMessage)
            {
                var message = Loc.GetString(radioMessage.Message, ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(spawn))));
                _radio.SendRadioMessage(spawn, message, radioMessage.Channel, spawn);
            }
            // Moffstation - End
        }
    }
}
