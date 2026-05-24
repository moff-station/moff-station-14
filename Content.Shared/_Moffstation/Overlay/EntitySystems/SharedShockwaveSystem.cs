

using Content.Shared._Moffstation.Overlay.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Overlay.EntitySystems;

/// <summary>
/// The Shared system which handles the triggering of the sonic boom overlay.
/// </summary>
public abstract partial class SharedShockwaveSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockwaveComponent, MapInitEvent>(OnMapInit);
    }

    protected void OnMapInit(Entity<ShockwaveComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.StartTime = _timing.CurTime;
        Dirty(entity,entity.Comp);
    }
}
