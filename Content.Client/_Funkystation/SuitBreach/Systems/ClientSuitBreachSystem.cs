using Content.Client._Starfall.Particles;
using Content.Shared._Funkystation.SuitBreach;
using Content.Shared._Funkystation.SuitBreach.Components;
using Content.Shared._Funkystation.SuitBreach.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client._Funkystation.SuitBreach.Systems;

public sealed partial class ClientSuitBreachSystem : SuitBreachSystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly Dictionary<EntityUid, ActiveEmitter?> _emitters = new();
    private readonly Dictionary<EntityUid, (EntityUid Stream, SuitBreachSeverity Severity)> _hiss = new();

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<SuitBreachedComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
        UpdateHiss(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SuitBreachedComponent> ent, ref ComponentShutdown args)
    {
        if (_emitters.Remove(ent.Owner, out var emitter))
            _particles.RemoveParticle(emitter);

        StopHiss(ent.Owner);
    }

    private void UpdateVisuals(Entity<SuitBreachedComponent> ent)
    {
        var shouldLeak = ent.Comp.IsLeaking;
        var isLeaking = _emitters.ContainsKey(ent.Owner);

        if (shouldLeak && !isLeaking)
        {
            var coords = _transform.GetMapCoordinates(ent.Owner);
            _emitters[ent.Owner] = _particles.SpawnEffect("SuitBreachAirLeak", coords, ent.Owner);
        }
        else if (!shouldLeak && isLeaking)
        {
            if (_emitters.Remove(ent.Owner, out var emitter))
                _particles.RemoveParticle(emitter);
        }
    }

    // looping hiss while leaking, louder the worse the breach
    private void UpdateHiss(Entity<SuitBreachedComponent> ent)
    {
        if (!ent.Comp.IsLeaking)
        {
            StopHiss(ent.Owner);
            return;
        }

        if (_hiss.TryGetValue(ent.Owner, out var playing) && playing.Severity == ent.Comp.Severity)
            return;

        StopHiss(ent.Owner);

        var volume = ent.Comp.HissVolumePerSeverity.GetValueOrDefault(ent.Comp.Severity, 0f);
        var audioParams = AudioParams.Default.WithLoop(true).WithVolume(volume);
        if (_audio.PlayEntity(ent.Comp.HissSound, Filter.Local(), ent.Owner, false, audioParams) is { } stream)
            _hiss[ent.Owner] = (stream.Entity, ent.Comp.Severity);
    }

    private void StopHiss(EntityUid suit)
    {
        if (_hiss.Remove(suit, out var playing))
            _audio.Stop(playing.Stream);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var (uid, emitter) in _emitters)
        {
            if (Deleted(uid) || emitter is not { Exhausted: false })
                continue;

            emitter.MapCoords = _transform.GetMapCoordinates(uid);
        }
    }
}
