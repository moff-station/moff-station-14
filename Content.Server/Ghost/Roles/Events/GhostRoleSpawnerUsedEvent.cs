using Content.Shared.Ghost.Roles.Components;

namespace Content.Server.Ghost.Roles.Events
{
    /// <summary>
    /// Raised on a spawned entity after they use a ghost role mob spawner.
    /// </summary>
    public sealed class GhostRoleSpawnerUsedEvent : EntityEventArgs
    {
        /// <summary>
        /// The entity that spawned this.
        /// </summary>
        public EntityUid Spawner;

        /// <summary>
        /// The entity spawned.
        /// </summary>
        public EntityUid Spawned;

        public GhostRoleSpawnerUsedEvent(EntityUid spawner, EntityUid spawned)
        {
            Spawner = spawner;

            Spawned = spawned;
        }
    }

    // Moffstation - Begin - Ghost role spawner usage tracking
    /// This event is raised on the entity with <see cref="GhostRoleMobSpawnerComponent"/> when it is used.
    [ByRefEvent]
    public readonly record struct UsedGhostRoleSpawnerEvent(EntityUid Spawned);
    // Moffstation - End
}
