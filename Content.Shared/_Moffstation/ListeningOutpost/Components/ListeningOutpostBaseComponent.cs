using Content.Shared.Cargo.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.ListeningOutpost.Components;

/// <summary>
/// Tags a grid as the listening outpost base, which causes it to become station-like and capable of using listening outpost trade functionality.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ListeningOutpostBaseComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public EntityUid AssociatedRule;
}

/// <summary>
/// Tags an entity as a listening outpost station, which may be made of many grids.
/// </summary>
[RegisterComponent, NetworkedComponent, UsedImplicitly]
public sealed partial class ListeningOutpostStationComponent : Component
{
    [DataField]
    public NetEntity? AssociatedRule;
}
