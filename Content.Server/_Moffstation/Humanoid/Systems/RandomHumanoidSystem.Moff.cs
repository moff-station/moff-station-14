using Content.Server.Humanoid.Components;
using Robust.Shared.Containers;

// ReSharper disable once CheckNamespace // Partial of upstream system
namespace Content.Server.Humanoid.Systems;

public sealed partial class RandomHumanoidSystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnSpawnerInserted(Entity<RandomHumanoidSpawnerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.Spawned is { } spawned && !TerminatingOrDeleted(spawned))
            _container.Insert(spawned, args.Container);
    }
}
