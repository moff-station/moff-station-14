namespace Content.Shared._Impstation.SpawnedFromTracker;

/// This component is used to track what created a spawner.
[RegisterComponent]
public sealed partial class SpawnedFromTrackerComponent : Component
{
    public EntityUid SpawnedFrom;
}

/// Raised on <see cref="SpawnedFromTrackerComponent.SpawnedFrom"/> when a tracked spawner is used, allowing the source
/// to react to the usage.
[ByRefEvent]
public readonly record struct TrackedSpawnerUsed(EntityUid Spawned, EntityUid Spawner);
