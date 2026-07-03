using Content.Shared._ES.Sparks;
using Content.Shared._ES.Sparks.Components;
using Content.Shared.Trigger;

namespace Content.Shared._Moffstation.Sparks;

/// <summary>
/// This system implements the behavior of <see cref="ESSparkOnTriggerComponent"/>. This component originally was
/// handled by the sparks system, but it didn't do standard <see cref="XOnTriggerSystem{T}"/> things, which is bad and I
/// hate it, so I rewrote it.
/// </summary>
public sealed partial class SparkOnTriggerSystem : XOnTriggerSystem<ESSparkOnTriggerComponent>
{
    [Dependency] private ESSparksSystem _sparks = default!;

    protected override void OnTrigger(Entity<ESSparkOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // TODO Sparks _should_ come out of `target`, but I'm not about to refactor the ES spark API right now.
        _sparks.DoSparks(ent);
    }
}
