using Content.Shared._Moffstation.ReadyManifest;

namespace Content.Client._Moffstation.ReadyManifest;

public sealed class ReadyManifestSystem : EntitySystem
{
    public void RequestReadyManifest()
    {
        RaiseNetworkEvent(new RequestReadyManifestMessage());
    }
}
