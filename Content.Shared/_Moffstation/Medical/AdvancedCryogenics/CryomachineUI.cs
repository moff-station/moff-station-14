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

    public CryomachineUiState(GasMixEntry gasMix)
    {
        //Capsule = capsule;
        GasMix = gasMix;
    }
}

