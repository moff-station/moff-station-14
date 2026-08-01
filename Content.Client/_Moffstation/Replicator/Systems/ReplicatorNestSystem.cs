using Content.Shared._Moffstation.Replicator.Components;
using Content.Shared._Moffstation.Replicator.Systems;

namespace Content.Client._Moffstation.Replicator.Systems;

public sealed partial class ReplicatorNestSystem : SharedReplicatorNestSystem
{
    protected override void ConvertTiles(Entity<ReplicatorNestComponent> ent, float radius)
    {
        // Nothing to do on the client
    }
}
