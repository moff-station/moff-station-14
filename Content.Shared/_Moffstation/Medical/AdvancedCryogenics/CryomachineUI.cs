using Content.Shared.Atmos.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;


[Serializable, NetSerializable]
public enum CryomachineUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class CryomachineSimpleUiMessage : BoundUserInterfaceMessage
{
    public enum MessageType { JumpstartBrain, DetachCapsule, EjectBeaker }

    public readonly MessageType Type;

    public CryomachineSimpleUiMessage(MessageType type)
    {
        Type = type;
    }
}

[Serializable, NetSerializable]
public sealed class CryomachineUiState : BoundUserInterfaceMessage
{
    // public readonly EntityUid? Capsule; // not serializable
    public readonly GasMixEntry GasMix;
    public readonly CryoCapsuleEntry CryoCapsule;
    public readonly NetEntity? CapsuleNetEnt;

    public CryomachineUiState(GasMixEntry gasMix, CryoCapsuleEntry cryoCapsule,  NetEntity? capsuleNetEnt)
    {
        GasMix = gasMix;
        CryoCapsule = cryoCapsule;
        CapsuleNetEnt = capsuleNetEnt;
    }
}

