using Content.Client._Funkystation.SuitBreach.Components;
using Content.Client._Starfall.Particles;
using Content.Shared._Funkystation.SuitBreach.Components;
using Content.Shared._Funkystation.SuitBreach.EntitySystems;
using Robust.Shared.Player;

namespace Content.Client._Funkystation.SuitBreach.Systems;

public sealed partial class ClientSuitBreachSystem : SuitBreachSystem
{
    [Dependency] private ParticleSystem _particles = default!;

    // this can run while state is being applied, where Audio.Stop does nothing, so delete the stream directly
    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SuitBreachedComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SuitBreachLeakVisualsComponent>(ent, out var visuals))
            return;

        _particles.RemoveParticle(visuals.Emitter);
        QueueDel(visuals.HissStream);
        RemCompDeferred(ent, visuals);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var ent in EntityQueryEnumerator<SuitBreachedComponent>())
        {
            if (!ent.Comp.IsLeaking)
            {
                if (TryComp<SuitBreachLeakVisualsComponent>(ent, out var stopped))
                {
                    _particles.RemoveParticle(stopped.Emitter);
                    Audio.Stop(stopped.HissStream);
                    RemCompDeferred(ent, stopped);
                }

                continue;
            }

            var visuals = EnsureComp<SuitBreachLeakVisualsComponent>(ent);
            visuals.Emitter ??= _particles.SpawnEffect(ent.Comp.LeakEffect, TransformSystem.GetMapCoordinates(ent), ent);
            visuals.HissStream ??= Audio.PlayEntity(ent.Comp.HissSound, Filter.Local(), ent, false)?.Entity;

            if (visuals.Emitter is { Exhausted: false } emitter)
                emitter.MapCoords = TransformSystem.GetMapCoordinates(ent);
        }
    }
}
