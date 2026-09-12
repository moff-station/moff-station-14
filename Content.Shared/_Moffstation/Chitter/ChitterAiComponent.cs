using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

// Lives directly on the station AI's held entity (see AiHeld in silicon.yml), alongside a ChitterAccountComponent,
// so the AI is its own Chitter account with no PDA or ID card needed - same idea as CommunicationsConsole/
// CrewMonitoringConsole already living straight on AiHeld instead of a separate console entity.
[RegisterComponent, NetworkedComponent]
public sealed partial class ChitterAiComponent : Component, IChitterSession
{
    public Guid? CurrentChatId { get; set; }

    // Separate cooldowns so a recent message send can't make a chat-creation request get silently dropped,
    // mirroring ChitterCartridgeComponent.
    public TimeSpan NextMessageAllowed { get; set; }
    public TimeSpan NextChatAllowed { get; set; }
}

[Serializable, NetSerializable]
public enum ChitterAiUiKey : byte
{
    Key,
}
