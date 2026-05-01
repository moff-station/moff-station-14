using Content.Shared._Moffstation.Voting.Components;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Voting.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class MoffEnrollEventSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoffEnrollEventComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<MoffEnrollEventComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MoffEnrollEventComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.EndTime)
                PredictedQueueDel(uid);
        }
    }
}
