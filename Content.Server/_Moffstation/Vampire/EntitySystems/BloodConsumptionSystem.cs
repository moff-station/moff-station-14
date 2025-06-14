using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Vampire.EntitySystems;

public sealed class BloodConsumptionSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodConsumptionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BloodConsumptionComponent? component, MapInitEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        component.NextUpdate = _timing.CurTime + component.UpdateInterval;

        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return;

        component.PrevBloodPercentage = _bloodstreamSystem.GetBloodLevelPercentage(uid, bloodstream);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var enumerator = EntityQueryEnumerator<BloodConsumptionComponent>();
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            Update(uid, comp, time);
        }
    }

    /// <summary>
    /// Sets the percentage values of hunger and thirst to a percentage of the bloodstream.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="comp"></param>
    /// <param name="time"></param>
    /// <remarks>
    /// </remarks>
    private void Update(EntityUid uid, BloodConsumptionComponent comp, TimeSpan time)
    {
        if (time < comp.NextUpdate)
            return;

        comp.NextUpdate += comp.UpdateInterval;

        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return; // we need at least the blood stream before we can do something.

        UpdateHungerThirst(uid, comp, bloodstream);
    }

    private void UpdateHungerThirst(EntityUid uid, BloodConsumptionComponent comp, BloodstreamComponent bloodstream)
    {
        var bloodstreamPercentage = _bloodstreamSystem.GetBloodLevelPercentage(uid, bloodstream);
        var modificationPercentage = Math.Clamp(
            bloodstreamPercentage - comp.PrevBloodPercentage,
            -comp.MaxChange,
            comp.MaxChange);
        if (TryComp<HungerComponent>(uid, out var hunger))
            _hungerSystem.ModifyHunger(uid, modificationPercentage * hunger.Thresholds[HungerThreshold.Overfed], hunger);
        if (TryComp<ThirstComponent>(uid, out var thirst))
            _thirstSystem.ModifyThirst(uid, thirst, modificationPercentage * thirst.ThirstThresholds[ThirstThreshold.OverHydrated]);
        comp.PrevBloodPercentage += modificationPercentage;
    }
}
