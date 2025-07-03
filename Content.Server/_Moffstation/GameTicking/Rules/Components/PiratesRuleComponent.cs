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

    /// <summary>
    /// The station associated with this rule
    /// </summary>
    [DataField]
    public EntityUid AssociatedStation;

    /// <summary>
    /// The total amount of money collected by the pirates
    /// </summary>
    [DataField]
    public int TotalMoneyCollected;
}
