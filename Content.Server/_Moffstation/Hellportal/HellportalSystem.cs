using System.Linq;
using Content.Server._Moffstation.Hellportal.Components;
using Content.Shared.EntityTable;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Moffstation.Hellportal;

public sealed partial class HellportalSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent < HellportalComponent, AnchorStateChangedEvent>(OnAnchorChange);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HellportalComponent, TransformComponent>();
        var totalCount = EntityQuery<HellportalMobComponent>().Count();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            comp.Accumulator += frameTime;
            var spawns = _entityTable.GetSpawns(comp.BasicSpawnTable);

            if (comp.Accumulator > comp.SpawnCooldown)
            {
                comp.Accumulator -= comp.SpawnCooldown;

                if (totalCount < comp.MaxSpawns)
                {
                    _audio.PlayPvs(comp.Sound, xform.Coordinates);
                    foreach (var proto in spawns)
                    {
                        Spawn(proto, xform.Coordinates);
                    }
                }
            }
        }
    }

    private void OnAnchorChange(EntityUid uid, HellportalComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            QueueDel(uid);
        }
    }
}
