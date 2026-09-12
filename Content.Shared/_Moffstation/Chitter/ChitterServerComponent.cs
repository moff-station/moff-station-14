namespace Content.Shared._Moffstation.Chitter;

[RegisterComponent]
public sealed partial class ChitterServerComponent : Component
{
    public readonly Dictionary<uint, ChitterAccount> Accounts = new();
    public readonly Dictionary<Guid, ChitterChat> Chats = new();
    public readonly Dictionary<Guid, ChitterChat> ArchivedChats = new();

    // Set once by ChitterServerSystem.OnEmagged. Every account this server knows about gets displayed
    // with a fake identity from then on (see ApplyEmagDisguise) - the real ID cards are never touched.
    public bool Emagged;
    public bool EmaggedClown;
}
