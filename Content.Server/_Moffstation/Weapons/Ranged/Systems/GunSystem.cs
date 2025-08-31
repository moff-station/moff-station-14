using System.Numerics;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Weapons.Ranged.Systems;

public sealed class GunSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, LandEvent>(FireOnLand);
    }

    private void FireOnLand(Entity<GunComponent> ent, ref LandEvent args)
    {
        if (!_random.Prob(ent.Comp.FireOnLandChance))
            return;

        EnsureComp<GunFireOnLandDischargingComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GunComponent, GunFireOnLandDischargingComponent>();
        while (query.MoveNext(out var uid, out var gun, out _))
        {
            RemCompDeferred<GunFireOnLandDischargingComponent>(uid);

            var direction = new Vector2(-gun.DefaultDirection.Y, gun.DefaultDirection.X);
            var coordinates = new EntityCoordinates(uid, direction);

            _gunSystem.AttemptShoot(uid, uid, gun, coordinates);
        }
    }
}

[RegisterComponent]
public sealed partial class GunFireOnLandDischargingComponent : Component;
