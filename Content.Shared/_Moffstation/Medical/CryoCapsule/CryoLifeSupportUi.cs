using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

[Serializable, NetSerializable]
public enum CryoLifeSupportUiKey : byte
{
    Key,
}


[Serializable, NetSerializable]
public sealed class CryoLifeSupportSimpleUiMessage(CryoLifeSupportSimpleUiMessage.ActionType action)
    : BoundUserInterfaceMessage
{
    public enum ActionType { ReviveBrain, EjectCapsule, EjectBeaker };

    public ActionType Action = action;
}

[Serializable, NetSerializable]
public sealed class CryoLifeSupportInjectUiMessage(int quantity) : BoundUserInterfaceMessage
{
    public int Quantity = quantity;
}
