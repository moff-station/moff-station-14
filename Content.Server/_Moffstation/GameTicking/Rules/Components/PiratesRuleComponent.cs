using Content.Server.GameTicking.Rules;
using Content.Server.Station;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(PiratesRuleSystem))]
public sealed partial class PiratesRuleComponent : Component
{
    /// <summary>
    /// Station config to apply to the shuttle, this is what gives it cargo functionality.
    /// </summary>
    [DataField]
    public StationConfig StationConfig = new()
    {
        StationPrototype = "PirateShuttleStation",
        StationComponentOverrides = new ComponentRegistry(),
    };
}
