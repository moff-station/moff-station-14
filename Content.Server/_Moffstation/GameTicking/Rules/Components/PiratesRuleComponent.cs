using Content.Server.GameTicking.Rules;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(PiratesRuleSystem))]
public sealed partial class PiratesRuleComponent : Component
{

    [DataField]
    public int StartingCash = 1000;

}
