using Content.Server._Moffstation.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared._Moffstation.Shuttles.Events;

namespace Content.Server._Moffstation.Spawners.EntitySystems;

/// <summary>
/// Handles the spawning of specific entities when an emergency shuttle launches.
/// </summary>
/// <inheritdoc/>
public sealed class SpawnOnEmergencyShuttleLaunchSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyShuttleLaunchEvent>(OnEmergencyShuttleLaunch);
    }

    /// <summary>
    /// Spawns the specified prototype when the emergency shuttle is launched.
    /// </summary>
    /// <param name="args"></param>
    /// <remarks>Shitcoded implementation, direct copy paste from <see cref="SpawnOnDespawnSystem"/>
    /// I'm not waiting a zillion years to make an upstream PR to deduplicate code.</remarks>
    private void OnEmergencyShuttleLaunch(ref EmergencyShuttleLaunchEvent args)
    {
        var query = EntityQueryEnumerator<SpawnOnEmergencyShuttleLaunchComponent, TransformComponent>();

        while (query.MoveNext(out _, out var comp, out var comp2))
        {
            Spawn(comp.Prototype, comp2.Coordinates);
        }
    }
}
