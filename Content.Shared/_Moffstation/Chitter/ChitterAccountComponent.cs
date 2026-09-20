using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Chitter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChitterAccountComponent : Component
{
    [DataField, AutoNetworkedField]
    public uint AccountId;

    [DataField, AutoNetworkedField]
    public string ProfilePictureId = string.Empty;

    // Accounts this one has chosen to block - never networked, since only the owning client's own
    // Chitter UI state (via ChitterUiState.BlockedContacts) needs to know about it.
    [DataField]
    public HashSet<uint> BlockedAccountIds = new();
}
