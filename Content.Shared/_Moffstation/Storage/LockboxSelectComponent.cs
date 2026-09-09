using Content.Shared.Access;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Storage;

/// <summary>
/// Marks a craftable generic lockbox that can be assigned to a department.
/// When a player interacts with it, they see a radial menu filtered to departments
/// they have access to. Selecting one replaces this entity with the department lockbox.
/// </summary>
[RegisterComponent]
public sealed partial class LockboxSelectComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<AccessLevelPrototype>, EntProtoId> Options = new();
}

[Serializable, NetSerializable]
public enum LockboxSelectUiKey : byte
{
    Key,
}


[Serializable, NetSerializable]
public sealed class LockboxSelectBoundUserInterfaceState(List<EntProtoId> options) : BoundUserInterfaceState
{
    public readonly List<EntProtoId> Options = options;
}

[Serializable, NetSerializable]
public sealed class LockboxSelectMessage(EntProtoId selectedProto) : BoundUserInterfaceMessage
{
    public readonly EntProtoId SelectedProto = selectedProto;
}
