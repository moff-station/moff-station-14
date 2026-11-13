using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Chaplain;

// <summary>
// Component that allows the altar to be swapped with other altar styles.
// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)] // ᓚᘏᗢ <(TRUE!
public sealed partial class AltarSwappingComponent : Component
{
    /// <summary>
    /// Altar entity prototypes that may be used as styles
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Exclusions = new();

    /// <summary>
    /// The altar prototype whose style is currently being copied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? CurrentStyle;
}

[Serializable, NetSerializable]
public enum AltarSwappingUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AltarSwappingSelectMessage(EntProtoId prototype) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Prototype = prototype;
}

[Serializable, NetSerializable]
public sealed class AltarSwappedEffectEvent(NetEntity altar) : EntityEventArgs
{
    public readonly NetEntity Altar = altar;
}
