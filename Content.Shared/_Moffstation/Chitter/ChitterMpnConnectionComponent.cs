using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Chitter;

// Lives on a PDA (or any other Chitter "loader") once it's been manually connected to a private M.P.N.
// server, overriding ChitterServerSystem's normal grid-based lookup until disconnected.
//
// ConnectedServer is networked directly (rather than relayed through PdaUpdateState) on purpose: the
// PDA's own outer chrome state and the active cartridge's UI state both ride on the exact same
// UserInterface slot (CartridgeLoaderComponent.UiKey), and only the last SetUiState call in a given
// tick actually reaches the client - so pushing the M.P.N. banner that way would always race the
// Chitter cartridge's own contact/chat list for the same slot and one of them would silently lose.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChitterMpnConnectionComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? ConnectedServer;

    public string ServerName = string.Empty;
}
