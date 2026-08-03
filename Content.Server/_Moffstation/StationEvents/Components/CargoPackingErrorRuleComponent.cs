using Content.Server._Moffstation.StationEvents.Events;
using Content.Shared.EntityTable;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.StationEvents.Components;

/// <summary>
/// <see cref="CargoPackingErrorRule"/>
/// </summary>
[RegisterComponent, Access(typeof(CargoPackingErrorRule))]
public sealed partial class CargoPackingErrorRuleComponent : Component
{
    /// <summary>
    /// Describes what entities this rule should add to cargo orders.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> SpawnTable;
}
