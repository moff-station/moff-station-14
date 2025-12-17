using Content.Shared.Objectives;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Objectives;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PotentialObjectivesComponent : Component
{
    /// <summary>
    /// The objective options presented to the player
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<(NetEntity netEntity, ObjectiveInfo info)> ObjectiveOptions = new();
}

/// <summary>
///     Clients listen for this event and when they get it, they open a popup so the player can fill out the objective summary.
/// </summary>
[Serializable, NetSerializable]
public sealed class ObjectivePickerOpenMessage : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class ObjectivePickerSelected : EntityEventArgs
{
    public NetEntity UserId;
    public HashSet<NetEntity> SelectedObjectives = new();
}
