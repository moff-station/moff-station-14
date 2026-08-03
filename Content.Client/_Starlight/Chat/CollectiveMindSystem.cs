using Content.Client.Chat.Managers;
using Content.Shared._Starlight.CollectiveMind;

namespace Content.Client._Starlight.Chat;

public sealed partial class CollectiveMindSystem : EntitySystem
{
    [Dependency] private IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CollectiveMindComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CollectiveMindComponent, ComponentRemove>(OnRemove);
    }

    private void OnInit(EntityUid uid, CollectiveMindComponent component, ComponentInit args)
    {
        _chatManager.UpdatePermissions();
    }

    private void OnRemove(EntityUid uid, CollectiveMindComponent component, ComponentRemove args)
    {
        _chatManager.UpdatePermissions();
    }
}
