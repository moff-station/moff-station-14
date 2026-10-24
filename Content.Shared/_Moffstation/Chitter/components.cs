using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Chitter;

/// Makes an entity the identity provider for a chitter user, meaning it stores information that is user-specific but
/// independent of current UI state (which is stored in <see cref="ChitterClientComponent"/>).
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ChitterIdentitySystem))]
public sealed partial class ChitterIdentityComponent : Component
{
    /// The GLOBALLY UNIQUE ID of this chitter user.
    [DataField, AutoNetworkedField]
    public ChitterAccountId AccountId;

    /// This user's avatar. If null, will be given a random avatar on connection to a server.
    [DataField, AutoNetworkedField]
    public ProtoId<ChitterAvatarPrototype>? Avatar;

    public static implicit operator ChitterAccountId(ChitterIdentityComponent c) => c.AccountId;
}

/// This component makes an entity into a Chitter server, meaning it tracks users, messages, etc.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(ChitterServerSystem))]
public sealed partial class ChitterServerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Discoverable = true;

    [DataField]
    public EntityTableSelector? EmagEffects;

    // Runtime data below

    [DataField, AutoNetworkedField]
    public Dictionary<ChitterAccountId, ChitterAccount> Accounts = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ChitterChatId, ChitterChat> Chats = new();

    [DataField, AutoNetworkedField]
    public ProtoId<ChitterServerEmagEffectPrototype>? CurrentEmagEffect;
}

/// Marks its owner as a chitter MPN server. Does nothing if it doesn't also have <see cref="ChitterServerComponent"/>
[RegisterComponent, NetworkedComponent, Access(typeof(ChitterMpnServerSystem))]
public sealed partial class ChitterMpnServerComponent : Component
{
    [DataField] public LocId ConnectedPopup = "chitter-mpn-connected";
}

/// Makes its owner a chitter client cartridge.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ChitterCartridgeSystem))]
public sealed partial class ChitterCartridgeComponent : ChitterClientComponent;

/// This component makes its entity able to use chitter intrinsicly, eg. for silicons.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(IntrinsicChitterClientSystem))]
public sealed partial class IntrinsicChitterClientComponent : ChitterClientComponent;

/// This abstract component makes an entity track transient chitter user data like the currently selected chat.
public abstract partial class ChitterClientComponent : Component
{
    /// The server this user is currently synched with.
    [DataField, AutoNetworkedField] public EntityUid? ConnectedServer;

    /// The chat the user is current looking at.
    [DataField, AutoNetworkedField] public ChitterChatId? CurrentChat;

    /// What this client is looking at in the UI.
    [DataField, AutoNetworkedField] public ChitterSubwindow? OpenSubwindow;

    /// How long the client must wait before sending messages to its connected server. Prevents message spam.
    [DataField, AutoNetworkedField] public TimeSpan ServerInteractionDelay = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public enum ChitterSubwindow : byte
{
    Settings,
    ServerSelection,
    NewChat,
}
