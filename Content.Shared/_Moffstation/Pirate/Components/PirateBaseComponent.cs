using Content.Shared.Cargo.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Pirate.Components;

/// <summary>
/// Tags a grid as the pirate base, which causes it to become station-like and capable of using pirate trade functionality.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PirateBaseComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid AssociatedRule;
}

/// <summary>
/// Tags an entity as a pirate station, which may be made of many grids.
/// </summary>
[RegisterComponent, NetworkedComponent, UsedImplicitly]
public sealed partial class PirateStationComponent : Component
{
    [DataField]
    public NetEntity? AssociatedRule;
}
