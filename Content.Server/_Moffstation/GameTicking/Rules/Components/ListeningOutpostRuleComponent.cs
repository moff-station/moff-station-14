namespace Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Shared.Station;
using Robust.Shared.Prototypes;

[RegisterComponent, Access(typeof(ListeningOutpostRuleSystem))]
public sealed partial class ListeningOutpostRuleComponent : Component
{

    /// <summary>
    /// Station config to apply to the outpost, this is what gives it cargo functionality for requisitions.
    /// </summary>
    [DataField]
    public StationConfig StationConfig = new()
    {
        StationPrototype = "ListeningOutpostStation",
        StationComponentOverrides = new ComponentRegistry(),
    };

    /// <summary>
    /// The Listening Outpost associated with this rule
    /// </summary>
    [DataField]
    public EntityUid AssociatedStation;

}
