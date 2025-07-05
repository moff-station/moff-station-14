using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Vampire.EntitySystems;

/// <summary>
/// This system handles entities with the
/// <see cref="Content.Shared._Moffstation.Vampire.Components.BloodConsumptionComponent"/>
/// and effectively manages their bloodstream.
/// The intention behind this is for vampire-like creatures to be able to use their bloodstream as
/// their reservoir for blood.
/// Blood in this case being their resource for spellcasting, healing, and is intended to be replenishable
/// by drinking the blood of other creatures (or eating blood packs).
/// </summary>
/// <remarks>
/// todo: set up proper adapter methods to interface with the bloodstream, for other systems to use.
/// </remarks>
public sealed class BloodConsumptionSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodConsumptionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<BloodConsumptionComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextUpdate = _timing.CurTime + entity.Comp.UpdateInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var enumerator = EntityQueryEnumerator<BloodConsumptionComponent>();
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            UpdateBloodConsumption((uid, comp), time);
        }
    }

    /// <summary>
    /// This method does a couple things on update:
    ///     - Calls <see cref="UpdateRegeneration"/> to drain the bloodstream slightly and heal if needed.
    ///     - Calls <see cref="UpdateHungerThirst"/> To set the percentage values of hunger and thirst to a percentage of the bloodstream.
    ///     - Calls <see cref="FlushTempSolution"/> Which flushes the temporary solution, preventing them from spilling blood on the ground.
    /// </summary>
    private void UpdateBloodConsumption(Entity<BloodConsumptionComponent> entity, TimeSpan time)
    {
        if (time < entity.Comp.NextUpdate)
            return;

        entity.Comp.NextUpdate += entity.Comp.UpdateInterval;

        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return; // we need at least the blood stream before we can do something.

        UpdateRegeneration(entity, bloodstream);
        UpdateHungerThirst(entity, bloodstream);
        FlushTempSolution((entity, bloodstream));
    }

    /// <summary>
    /// Updates the vampire's hunger and thirst values periodically based on the current blood level percentage.
    /// The hunger and thirst values are limited in how fast they can change via the
    /// <see cref="BloodConsumptionComponent.MaxChange"/> value.
    /// </summary>
    private void UpdateHungerThirst(Entity<BloodConsumptionComponent> entity, BloodstreamComponent bloodstream)
    {
        var bloodstreamPercentage = _bloodstreamSystem.GetBloodLevelPercentage((entity, bloodstream));
        var modificationPercentage = Math.Clamp(
            bloodstreamPercentage - entity.Comp.PrevBloodPercentage,
            -entity.Comp.MaxChange,
            entity.Comp.MaxChange);
        if (TryComp<HungerComponent>(entity, out var hunger))
            _hungerSystem.ModifyHunger(entity, modificationPercentage * hunger.Thresholds[HungerThreshold.Overfed], hunger);
        if (TryComp<ThirstComponent>(entity, out var thirst))
            _thirstSystem.ModifyThirst(entity, thirst, modificationPercentage * thirst.ThirstThresholds[ThirstThreshold.OverHydrated]);
        entity.Comp.PrevBloodPercentage += modificationPercentage;
    }

    /// <summary>
    /// Updates the vampire's bloodstream according to whether they are healing or not. Also performs their healing.
    /// </summary>
    private void UpdateRegeneration(Entity<BloodConsumptionComponent> entity, BloodstreamComponent bloodstream)
    {
        // check damage
        if (TryComp<DamageableComponent>(entity.Owner, out var damage)
	    && damage.Damage.AnyPositive()) // Vampires should be able to heal all damage types
        {
	        // heal according to comp amount
	        _damageSystem.TryChangeDamage(entity.Owner, entity.Comp.HealPerUpdate, true, false, damage);
	        // subtract blood for healing
	        _bloodstreamSystem.TryModifyBloodLevel((entity.Owner, bloodstream), entity.Comp.HealingBloodlossPerUpdate);
	        return;
        }
        // else subtract the usual amount of blood
        _bloodstreamSystem.TryModifyBloodLevel((entity.Owner, bloodstream), entity.Comp.BaseBloodlossPerUpdate);
    }

    /// <summary>
    /// Clear the temporary solution of the Bloodstream.
    /// This is an in-elegant method to avoid the vampire spilling their blood all over the floor.
    /// </summary>
    /// <remarks>
    /// todo: Make this a bit more elegant, and maybe introduce a method where vampires will spill reagents that
    /// their body rejects (food, drinks, basically anything that isn't blood). The problem is that the bloodstream
    /// will fail to properly initialize if we nullify the temporary solution, so this may require some changes
    /// to bloodstreams themselves to accept the ability to not have a temporary solution.
    /// </remarks>
    private void FlushTempSolution(Entity<BloodstreamComponent> entity)
    {
        if (!_solutionContainerSystem.ResolveSolution(entity.Owner,
                entity.Comp.BloodTemporarySolutionName,
                ref entity.Comp.TemporarySolution,
                out var tempSolution))
            return;
        tempSolution.RemoveAllSolution();
    }
}
