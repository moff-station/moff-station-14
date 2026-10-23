using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.GaussFabricator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
[Access(typeof(GaussFabricatorSystem))]
public sealed partial class GaussFabricatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// Currently configured power consumption in Watts.
    [DataField, AutoNetworkedField]
    public float CurrentDraw = 10000f;

    /// Fraction of received power added as heat to the surrounding atmosphere while running.
    [DataField]
    public float HeatMultiplier = 0.05f;

    /// Minimum value <see cref="CurrentDraw" /> can be set to.
    [DataField]
    public float MinDraw;

    /// Maximum value <see cref="CurrentDraw" /> can be set to.
    [DataField]
    public float MaxDraw = 250000f;

    /// Atmos temperature the machine can operate in, in kelvin. It runs badly outside it.
    [DataField]
    public MinMax TemperatureAcceptable = new(20, 200);

    /// Atmos temperature the machine runs optimally in, in kelvin.
    [DataField]
    public MinMax TemperatureOptimal = new(60, 130);

    /// Atmos pressure the machine can operate in, in kPa. It runs badly outside it.
    [DataField]
    public MinMax PressureAcceptable = new(20, 300);

    /// Atmos pressure the machine runs optimally in, in kPa.
    [DataField]
    public MinMax PressureOptimal = new(80, 120);

    /// Output speed multiplier while the machine runs outside an acceptable range.
    [DataField]
    public float BadMultiplier = 0.2f;

    /// Output speed multiplier while the machine runs inside an optimal range.
    [DataField]
    public float OptimalMultiplier = 1.5f;

    // Not a DataField: UI-only, so the UI should refresh straight away after a load.
    [ViewVariables]
    public TimeSpan NextUiUpdate;
}
