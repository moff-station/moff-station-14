using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Moffstation.GaussFabricator;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.GaussFabricator;

public sealed partial class GaussFabricatorSystem : SharedGaussFabricatorSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;

    [Dependency] private EntityQuery<BatteryComponent> _batteryQuery = default!;
    [Dependency] private EntityQuery<PowerNetworkBatteryComponent> _powerBatteryQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));
    }

    [SubscribeLocalEvent]
    private void OnUiOpened(Entity<GaussFabricatorComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    protected override void ExamineAtmosphere(Entity<GaussFabricatorComponent> ent, ExaminedEvent args)
    {
        var mixture = _atmosphere.GetContainingMixture(ent.Owner);
        var temperature = GetBand(ent.Comp.TemperatureAcceptable, ent.Comp.TemperatureOptimal, mixture?.Temperature);
        var pressure = GetBand(ent.Comp.PressureAcceptable, ent.Comp.PressureOptimal, mixture?.Pressure);

        args.PushMarkup(Loc.GetString("gauss-fabricator-examine-temperature", ("band", temperature.ToString())));
        args.PushMarkup(Loc.GetString("gauss-fabricator-examine-pressure", ("band", pressure.ToString())));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in EntityQueryEnumerator<GaussFabricatorComponent, PowerNetworkBatteryComponent, TransformComponent>())
        {
            ent.Comp2.MaxChargeRate = ent.Comp1.DrawRate;
            ent.Comp2.Enabled = ent.Comp1.Enabled;

            var mixture = _atmosphere.GetContainingMixture((ent.Owner, ent.Comp3), excite: ent.Comp2.Enabled);

            ent.Comp2.Efficiency =
                GetBandMultiplier(ent.Comp1, ent.Comp1.TemperatureAcceptable, ent.Comp1.TemperatureOptimal, mixture?.Temperature)
                * GetBandMultiplier(ent.Comp1, ent.Comp1.PressureAcceptable, ent.Comp1.PressureOptimal, mixture?.Pressure);

            var received = ent.Comp2.CurrentReceiving;
            var powered = ent.Comp2.Enabled && received > 0f;
            _ambient.SetAmbience(ent.Owner, powered);
            _appearance.SetData(ent.Owner, PowerDeviceVisuals.Powered, powered);
            UpdateLight((ent.Owner, ent.Comp1), powered, received);

            if (_timing.CurTime >= ent.Comp1.NextUiUpdate)
            {
                ent.Comp1.NextUiUpdate = _timing.CurTime + ent.Comp1.UiUpdateInterval;
                UpdateUi((ent.Owner, ent.Comp1, ent.Comp2), mixture);
            }

            if (!ent.Comp2.Enabled)
                continue;

            if (received > 0f && mixture != null)
                _atmosphere.AddHeat(mixture, received * ent.Comp1.HeatMultiplier * frameTime);
        }
    }

    private void UpdateLight(Entity<GaussFabricatorComponent> ent, bool powered, float received)
    {
        if (!_lights.TryGetLight(ent, out var light))
            return;

        _lights.SetEnabled(ent, powered, light);
        if (!powered)
            return;

        var fraction = ent.Comp.MaxDrawRate > 0f ? Math.Clamp(received / ent.Comp.MaxDrawRate, 0f, 1f) : 1f;
        _lights.SetEnergy(ent, MathHelper.Lerp(ent.Comp.LightEnergy.Min, ent.Comp.LightEnergy.Max, fraction), light);
        _lights.SetRadius(ent, MathHelper.Lerp(ent.Comp.LightRadius.Min, ent.Comp.LightRadius.Max, fraction), light);
    }

    /// <summary>
    /// Multiplier for the progress for a specific band. No atmos counts as bad.
    /// </summary>
    private static float GetBandMultiplier(
        GaussFabricatorComponent comp,
        MinMax acceptable,
        MinMax optimal,
        float? value)
    {
        return GetBand(acceptable, optimal, value) switch
        {
            Band.Optimal => comp.OptimalMultiplier,
            Band.Acceptable => comp.NormalMultiplier,
            _ => comp.BadMultiplier,
        };
    }

    private static Band GetBand(MinMax acceptable, MinMax optimal, float? value)
    {
        if (value is not { } reading || reading < acceptable.Min || reading > acceptable.Max)
            return Band.Bad;

        return reading >= optimal.Min && reading <= optimal.Max
            ? Band.Optimal
            : Band.Acceptable;
    }

    protected override void UpdateUi(Entity<GaussFabricatorComponent> ent)
    {
        if (_powerBatteryQuery.TryComp(ent, out var pnb))
            UpdateUi((ent.Owner, ent.Comp, pnb), _atmosphere.GetContainingMixture(ent.Owner));
    }

    private void UpdateUi(Entity<GaussFabricatorComponent, PowerNetworkBatteryComponent> ent, GasMixture? mixture)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, GaussFabricatorUiKey.Key)
            || !_batteryQuery.TryComp(ent, out var battery))
            return;

        // One thingy is spawned per full battery.
        var outputRate = battery.MaxCharge > 0f
            ? ent.Comp2.CurrentReceiving * ent.Comp2.Efficiency * 60f / battery.MaxCharge
            : 0f;

        _uiSystem.SetUiState(
            ent.Owner,
            GaussFabricatorUiKey.Key,
            new GaussFabricatorBuiState(
                ent.Comp2.CurrentReceiving,
                _battery.GetChargeLevel((ent.Owner, battery)),
                outputRate,
                mixture?.Temperature,
                mixture?.Pressure));
    }

    private enum Band : byte
    {
        Bad,
        Acceptable,
        Optimal,
    }
}
