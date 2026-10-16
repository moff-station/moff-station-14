using Content.Shared._Moffstation.Chitter;

namespace Content.Client._Moffstation.Chitter;

public sealed partial class ClientChitterIdentitySystem : ChitterIdentitySystem
{
    // Return garbage data, it'll be updated by the server.
    protected override ChitterAccountId GetNewId() => default;
}
