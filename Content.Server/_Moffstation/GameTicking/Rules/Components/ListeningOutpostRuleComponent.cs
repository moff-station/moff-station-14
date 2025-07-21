using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(PiratesRuleSystem))]

public sealed partial class ListeningOutpostRuleComponent : Component
{
    /// <summary>
    /// Station config to apply to the shuttle, this is what gives it cargo functionality.
    /// </summary>
    [DataField]
    public string ShuttleGridPath = "/Maps/Shuttles/trading_outpost.yml";
}
