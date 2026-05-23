using Content.Shared._Moffstation.Overlay.Components;
using Content.Shared._Moffstation.Overlay.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Overlay.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public abstract partial class SharedShockwaveSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockwaveComponent, ShockwaveEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<ShockwaveComponent> entity, ref ShockwaveEvent args)
    {
        if (args.Handled)
            return;
        Activate(entity);
        args.Handled = true;
    }

    public void UpdateComponents()
    {
        var enumerator = _entityManager.AllEntityQueryEnumerator<ShockwaveComponent>();
        while (enumerator.MoveNext(out var entity, out var comp))
        {
            UpdateActive((entity, comp));
        }
    }

    public void Activate(Entity<ShockwaveComponent> entity)
    {
        entity.Comp.Active = true;
        entity.Comp.StartTime = _timing.CurTime;
    }

    public void UpdateActive(Entity<ShockwaveComponent> entity)
    {
        if (entity.Comp.Active && (entity.Comp.StartTime + entity.Comp.Duration) > _timing.CurTime)
            Deactivate(entity);
    }

    public void Deactivate(Entity<ShockwaveComponent> entity)
    {
        entity.Comp.Active = false;
    }
}
