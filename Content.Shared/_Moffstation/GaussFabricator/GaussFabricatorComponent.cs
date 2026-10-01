using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.GaussFabricator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedGaussFabricatorSystem))]
public sealed partial class GaussFabricatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Configured draw rate in watts, applied to the power network battery by the server.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DrawRate = 10000f;

    /// <summary>
    /// Fraction of received power (W) added as heat (J/s) to the surrounding atmosphere.
    /// </summary>
    [DataField]
    public float HeatMultiplier = 0.05f;

    /// <summary>
    /// Minimum draw rate players can configure in watts
    /// </summary>
    [DataField]
    public float MinDrawRate;

    /// <summary>
    /// Maximum draw rate players can configure in watts
    /// </summary>
    [DataField]
    public float MaxDrawRate = 250000f;

    /// <summary>
    /// Atmos temperature where the reading is acceptable, in kelvin. Outside it is bad.
    /// </summary>
    [DataField]
    public MinMax TemperatureAcceptable = new(20, 200);

    /// <summary>
    /// Atmos temperature where the reading counts as optimal, in kelvin.
    /// </summary>
    [DataField]
    public MinMax TemperatureOptimal = new(60, 130);

    /// <summary>
    /// Atmos pressure where the reading is acceptable, in kPa. Outside it is bad.
    /// </summary>
    [DataField]
    public MinMax PressureAcceptable = new(20, 300);

    /// <summary>
    /// Atmos pressure where the reading counts as optimal, in kPa.
    /// </summary>
    [DataField]
    public MinMax PressureOptimal = new(80, 120);

    /// <summary>
    /// Output speed multiplier for a reading outside its acceptable range.
    /// </summary>
    [DataField]
    public float BadMultiplier = 0.2f;

    /// <summary>
    /// Output speed multiplier for a reading that is acceptable but not optimal.
    /// </summary>
    [DataField]
    public float NormalMultiplier = 1f;

    /// <summary>
    /// Output speed multiplier for a reading inside its optimal range.
    /// </summary>
    [DataField]
    public float OptimalMultiplier = 1.5f;

    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUiUpdate;
}
