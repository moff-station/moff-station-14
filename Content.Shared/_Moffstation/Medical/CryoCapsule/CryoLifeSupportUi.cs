using Content.Shared._Moffstation.Body.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

[Serializable, NetSerializable]
public enum CryoLifeSupportUiKey : byte
{
    Key,
}

/// <summary>
/// Sent when an action with no parameter is requested from the UI
/// </summary>
/// <param name="action"></param>
[Serializable, NetSerializable]
public sealed class CryoLifeSupportSimpleUiMessage(CryoLifeSupportSimpleUiMessage.ActionType action)
    : BoundUserInterfaceMessage
{
    public enum ActionType { ReviveBrain, EjectCapsule, EjectBeaker };

    public ActionType Action = action;
}

/// <summary>
/// Sent when an injection is requested from the UI
/// </summary>
/// <param name="quantity"></param>
[Serializable, NetSerializable]
public sealed class CryoLifeSupportInjectUiMessage(int quantity) : BoundUserInterfaceMessage
{
    public int Quantity = quantity;
}

/// <summary>
/// Send to update the state of the UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class CryoLifeSupportUiState(
    GasMixEntry gasMix,
    FixedPoint2? reagentsCapacity,
    List<ReagentQuantity>? reagents,
    NetEntity? capsuleEntity,
    List<(string,OrganEntry)>? organs)
    : BoundUserInterfaceMessage
{
    public readonly GasMixEntry GasMix = gasMix;
    public readonly FixedPoint2? ReagentsCapacity = reagentsCapacity;
    public readonly List<ReagentQuantity>? Reagents = reagents;
    public readonly NetEntity? CapsuleEntity = capsuleEntity;
    public readonly List<(string,OrganEntry)>? Organs = organs;
}
