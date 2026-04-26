using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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
    public readonly GasMixEntry GasMix;
    public readonly CryoCapsuleEntry CryoCapsule;
    public readonly NetEntity? CapsuleNetEnt;  // TODO : probably there is a better way to share this info.
    public readonly FixedPoint2? BeakerCapacity;
    public readonly List<ReagentQuantity>? Beaker;
    //private List<ReagentQuantity>? Injecting;

    public CryomachineUiState(
        GasMixEntry gasMix,
        CryoCapsuleEntry cryoCapsule,
        NetEntity? capsuleNetEnt,
        FixedPoint2? beakerCapacity,
        List<ReagentQuantity>? beaker)
    {
        GasMix = gasMix;
        CryoCapsule = cryoCapsule;
        CapsuleNetEnt = capsuleNetEnt;
        BeakerCapacity = beakerCapacity;
        Beaker = beaker;
    }
}

