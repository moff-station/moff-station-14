using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.Timing;
using Content.Server.Radiation.Systems;
using Content.Server.Radiation.Components;


namespace Content.Server._Funkystation.Atmos.Reactions;

/// <summary>
///     Funky Atmos - /tg/ gases
///     Consumes a tiny amount of tritium to convert CO2 and oxygen to pluoxium.
/// </summary>
[UsedImplicitly]
public sealed partial class PluoxiumRadiationProductionReaction : IGasReactionEffect
{
    private static IEntityManager EntityManager => IoCManager.Resolve<IEntityManager>();
    private static IGameTiming GameTiming => IoCManager.Resolve<IGameTiming>();
    private static RadiationSystem RadiationSystem => EntityManager.System<RadiationSystem>();
    private static SharedMapSystem MapSystem => EntityManager.System<SharedMapSystem>();
    private const float RadiationThreshold = 0.01f;
    private static readonly TimeSpan TimerDuration = TimeSpan.FromSeconds(5);

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        float initO2 = mixture.GetMoles(Gas.Oxygen);
        float initCO2 = mixture.GetMoles(Gas.CarbonDioxide);

        float radiationLevel = 0f;

        if (holder is null)
        {
            return ReactionResult.NoReaction;
        }
        else if (holder is IComponent component)
        {
            radiationLevel = GetRadiationLevel(component.Owner);
        }
        else if (holder is TileAtmosphere tile)
        {
            var tileRef = atmosphereSystem.GetTileRef(tile);

            // We use the center of the tile to ensure the Raycast actually hits the intended area
            var coords = MapSystem.ToCenterCoordinates(tileRef);
            radiationLevel = RadiationSystem.GetRadiationAtCoordinates(coords);

            // If we have no data, or data is too low, we MUST request a sample for the future
            if (radiationLevel < RadiationThreshold)
            {
                RadiationSystem.RequestTileRadiationSampling(coords);
                return ReactionResult.Reacting;
            }
        }
        else if (holder is PipeNet pipeNet) // Very resource heavy. Could be disabled or commented out with flavor being pipes have some kind of shielding.
        {
            float totalRads = 0f;
            int totalNodes = pipeNet.Nodes.Count;

            if (totalNodes == 0)
                return ReactionResult.NoReaction;

            var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();

            foreach (var node in pipeNet.Nodes)
            {
                if (!xformQuery.TryGetComponent(node.Owner, out var xform))
                    continue;

                var coords = xform.Coordinates;
                var nodeRads = RadiationSystem.GetRadiationAtCoordinates(coords);

                // Always request sampling so the cache stays fresh for the next atmos tick
                if (nodeRads < 0.001f)
                    RadiationSystem.RequestTileRadiationSampling(coords);

                // Add the rads to the total
                totalRads += nodeRads;
            }

            // Average across the entire network
            radiationLevel = totalRads / totalNodes;
        }

        if (radiationLevel < RadiationThreshold)
            return ReactionResult.Reacting;

        float producedAmount = Math.Min(radiationLevel, Math.Min(initCO2, initO2 * 2f));

        float co2Removed = producedAmount;
        float oxyRemoved = producedAmount * 0.5f;

        if (co2Removed > initCO2 ||
            oxyRemoved > initO2)
            return ReactionResult.NoReaction;

        if (producedAmount <= 0)
            return ReactionResult.Reacting;

        mixture.AdjustMoles(Gas.CarbonDioxide, -co2Removed);
        mixture.AdjustMoles(Gas.Oxygen, -oxyRemoved);
        mixture.AdjustMoles(Gas.Pluoxium, producedAmount);

        float energyReleased = producedAmount * Atmospherics.PluoxiumProductionEnergy;
        float heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }

    private float GetRadiationLevel(EntityUid entity)
    {
        bool hadReceiver = EntityManager.HasComponent<RadiationReceiverComponent>(entity);
        var receiverComp = EntityManager.EnsureComponent<RadiationReceiverComponent>(entity);

        if (!hadReceiver)
        {
            var timerComp = EntityManager.EnsureComponent<RadiationReceiverTimerComponent>(entity);
            timerComp.TimerExpiresAt = GameTiming.CurTime + TimerDuration;
        }
        else if (EntityManager.TryGetComponent<RadiationReceiverTimerComponent>(entity, out var timerComp))
        {
            timerComp.TimerExpiresAt = GameTiming.CurTime + TimerDuration;
        }

        return receiverComp.CurrentRadiation;
    }
}

[RegisterComponent]
public sealed partial class RadiationReceiverTimerComponent : Component
{
    public TimeSpan TimerExpiresAt { get; set; } = TimeSpan.Zero;
}

public sealed partial class RadiationTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var timer in EntityQuery<RadiationReceiverTimerComponent>())
        {
            var uid = timer.Owner;
            if (_timing.CurTime >= timer.TimerExpiresAt)
            {
                RemComp<RadiationReceiverComponent>(uid);
                RemComp<RadiationReceiverTimerComponent>(uid);
            }
        }
    }
}
