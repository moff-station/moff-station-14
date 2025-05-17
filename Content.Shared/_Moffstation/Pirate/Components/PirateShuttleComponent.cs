using Content.Shared._Moffstation.Cargo.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Pirate.Components;

/// <summary>
/// Tags grid as nuke ops shuttle
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PirateShuttleComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid AssociatedRule;

    /// <summary>
    /// How much cash the shuttle has collected
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public double Money;
}

[ByRefEvent]
public record struct FulfillPirateOrderEvent(Entity<PirateShuttleComponent> Shuttle, CargoOrderData Order, Entity<CargoOrderConsoleComponent> OrderConsole)
{
    public Entity<CargoOrderConsoleComponent> OrderConsole = OrderConsole;
    public Entity<PirateShuttleComponent> Shuttle = Shuttle;
    public CargoOrderData Order = Order;

    public EntityUid? FulfillmentEntity;
    public bool Handled = false;
}
