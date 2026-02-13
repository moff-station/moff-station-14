using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(StowawaysRuleSystem))]
public sealed partial class StowawaysRuleComponent : Component
{
    /// <summary>
    /// How much of the crew is allowed to be stowaways, maximum? 
    /// </summary>
    [DataField]
    public float PlayerRatio = 0.25f;

    /// <summary>
    /// What's the prototype of the Stowaway job? 
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;
}
