using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared._Funkystation.SuitBreach;
using Content.Shared._Funkystation.SuitBreach.Components;
using Content.Shared._Funkystation.SuitBreach.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Temperature;
using Robust.Shared.Timing;

namespace Content.Server._Funkystation.SuitBreach;

public sealed partial class ServerSuitBreachSystem : SuitBreachSystem
{
    [Dependency] private GasTankSystem _gasTank = default!;
    [Dependency] private BarotraumaSystem _barotrauma = default!; // Moff
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<TemperatureProtectionComponent> _temperatureProtectionQuery; // Moff

    private static readonly TimeSpan DrainInterval = TimeSpan.FromSeconds(1);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        foreach (var ent in EntityQueryEnumerator<SuitBreachedComponent>())
        {
            if (!Enabled)
            {
                SetLeaking(ent, false);
                continue;
            }

            if (curTime < ent.Comp.NextDrain)
                continue;

            ent.Comp.NextDrain = curTime + DrainInterval;
            DrainTank(ent);
        }
    }

    // drains the connected tank at the current severity's rate
    private void DrainTank(Entity<SuitBreachedComponent> suit)
    {
        if (!TryGetWearer(suit.Owner, out var wearerUid) || // Moff - don't assume outerclothing
            !TryComp<InternalsComponent>(wearerUid, out var internals) ||
            !TryComp<GasTankComponent>(internals.GasTankEntity, out var tank))
        {
            SetLeaking(suit, false);
            return;
        }

        var rate = suit.Comp.DrainRatesPerSecond.GetValueOrDefault(suit.Comp.Severity, 0f);
        if (rate > 0 && tank.Air.TotalMoles > 0)
            _gasTank.RemoveAir((internals.GasTankEntity.Value, tank), rate * (float) DrainInterval.TotalSeconds);

        SetLeaking(suit, tank.Air.TotalMoles > 0);
    }

    protected override void OnSeverityChanged(EntityUid suit)
    {
        if (TryGetWearer(suit, out var wearer))
            _barotrauma.RefreshPressureProtection(wearer);
    }

    [SubscribeLocalEvent]
    private void OnGetPressureProtection(Entity<SuitBreachedComponent> ent, ref GetPressureProtectionValuesEvent args)
    {
        if (ent.Comp.Severity != SuitBreachSeverity.Catastrophic)
            return;

        args.HighPressureMultiplier = 1f;
        args.HighPressureModifier = 0f;
        args.LowPressureMultiplier = 1f;
        args.LowPressureModifier = 0f;
    }

    // Moff start - Temperature protection goes through BeforeHeatExchangeEvent upstream
    // [SubscribeLocalEvent]
    // private void OnGetTemperatureProtection(Entity<SuitBreachedComponent> ent, ref GetTemperatureProtectionEvent args)
    // {
    //     if (ent.Comp.Severity != SuitBreachSeverity.Catastrophic)
    //         return;
    //
    //     args.Coefficient = 1f;
    // }

    // undoes the suit's TemperatureProtection multiplier
    [SubscribeLocalEvent]
    private void OnBeforeHeatExchange(Entity<SuitBreachedComponent> ent, ref InventoryRelayedEvent<BeforeHeatExchangeEvent> args)
    {
        if (ent.Comp.Severity != SuitBreachSeverity.Catastrophic
            || !_temperatureProtectionQuery.TryComp(ent, out var protection)
            || protection.Coefficient <= 0f)
            return;

        args.Args.HeatTransferModifier /= protection.Coefficient;
    }
    // Moff end
}
