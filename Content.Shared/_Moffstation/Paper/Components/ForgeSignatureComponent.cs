using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Paper.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ForgeSignatureComponent : Component
{
    /// <summary>
    /// The the signature written when a paper is signed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    [AutoNetworkedField]
    public string Signature = "";
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
