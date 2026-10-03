using Content.Server._Moffstation.Anomaly.AnomalyCreature.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Light.Components;

namespace Content.Server._Moffstation.Anomaly.AnomalyCreature.Systems;

public sealed partial class LightExtinguisherSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private PoweredLightSystem _poweredLight = default!;

    public override void Update(float frameTime)
    {

        DisableNearbyLightbulbs(EntityUid uid);

    }

    internal void DisableNearbyLightbulbs(Entity<LightExtinguisherComponent> ent)
    {
        foreach (var light in _entityLookup.GetEntitiesInRange<PoweredLightComponent>(ent.Owner.ToCoordinates(), ent.Comp.Radius))
        {
            _poweredLight.TryDestroyBulb(light);
        }
    }
}
