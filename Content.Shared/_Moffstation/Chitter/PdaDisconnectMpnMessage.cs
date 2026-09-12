using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

// Sent from the "Leave Network" button on the PDA's own M.P.N. status banner - just another PDA UI
// message, same as PdaToggleFlashlightMessage and friends.
[Serializable, NetSerializable]
public sealed class PdaDisconnectMpnMessage : BoundUserInterfaceMessage
{
    public PdaDisconnectMpnMessage() { }
}
