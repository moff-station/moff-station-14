using Content.Server.GameTicking.Rules;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(PiratesRuleSystem))]
public sealed partial class PiratesRuleComponent : Component
{
    /// <summary>
    /// Station config to apply to the shuttle, this is what gives it cargo functionality.
    /// </summary>
    [DataField("stationProto")]
    public string StationPrototype = "PirateShuttleStation";
}
