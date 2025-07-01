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
    public string? Signature;

    /// <summary>
    /// The maximum amount of characters allowed in a battlecry
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public int MaxSignatureLength = 30;
}

/// <summary>
/// Key representing which <see cref="PlayerBoundUserInterface"/> is currently open.
/// Useful when there are multiple UI for an object. Here it's future-proofing only.
/// </summary>
[Serializable, NetSerializable]
public enum ForgeSignatureUiKey : byte
{
    Key,
}

/// <summary>
/// Represents an <see cref="MeleeSpeechComponent"/> state that can be sent to the client
/// </summary>
[Serializable, NetSerializable]
public sealed class ForgeSignatureBoundUserInterfaceState : BoundUserInterfaceState
{
    public string CurrentSignature { get; }
    public ForgeSignatureBoundUserInterfaceState(string currentSignature)
    {
        CurrentSignature = currentSignature;
    }
}

[Serializable, NetSerializable]
public sealed class SignatureChangedMessage : BoundUserInterfaceMessage
{
    public string Signature { get; }
    public SignatureChangedMessage(string signature)
    {
        Signature = signature;
    }
}
