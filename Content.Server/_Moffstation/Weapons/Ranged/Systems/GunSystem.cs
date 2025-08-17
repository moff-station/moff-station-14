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

        var direction = new Vector2(-ent.Comp.DefaultDirection.Y, ent.Comp.DefaultDirection.X);
        var coordinates = new EntityCoordinates(ent, direction);

        _gunSystem.AttemptShoot(ent.Owner, ent.Owner, ent.Comp, coordinates);
    }
}
