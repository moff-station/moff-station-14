using Content.Shared.Cargo.Prototypes;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Station;
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
    /// The pirate station associated with this rule
    /// </summary>
    [DataField]
    public EntityUid AssociatedStation;


    /// <summary>
    /// The total amount of money collected by the pirates
    /// </summary>
    [DataField]
    public int TotalMoneyCollected;

    /// <summary>
    /// Tracks the previous balance of the accounts, so that the total amount of money earned can be calculated
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CargoAccountPrototype>, int> LastBalance = new();

    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "Pirate";
}
