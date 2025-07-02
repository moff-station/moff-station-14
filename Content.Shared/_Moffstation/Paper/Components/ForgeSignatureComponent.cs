using Content.Shared.Speech.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Paper.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ForgeSignatureComponent : Component
{
    /// <summary>
    /// The battlecry to be said when an entity attacks with this component
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    [AutoNetworkedField]
    public string Signature = "";

    /// <summary>
    /// The maximum amount of characters allowed in a battlecry
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    [AutoNetworkedField]
    public int MaxSignatureLength = 30;
}

[Serializable, NetSerializable]
public enum ForgeSignatureUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ForgedSignatureChangedMessage(string signature) : BoundUserInterfaceMessage
{
    public string Signature { get; } = signature;
}
