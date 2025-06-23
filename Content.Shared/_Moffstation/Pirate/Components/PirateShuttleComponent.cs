using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Pirate.Components;

/// <summary>
/// Tags grid as pirate shuttle
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PirateShuttleComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid AssociatedRule;
}

/// <summary>
/// Tags an entity as a pirate station, which may be made of many grids.
/// </summary>
[RegisterComponent, NetworkedComponent, UsedImplicitly]
public sealed partial class PirateStationComponent : Component;
