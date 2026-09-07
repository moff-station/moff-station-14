using Content.Client.UserInterface.Systems.Character;
using Content.Shared._Moffstation.CharacterMenu;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.CharacterMenu;

/// <summary>
/// Joe Biden please help me open this window
/// </summary>
public sealed partial class MoffCharacterWindowSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;

    [SubscribeNetworkEvent]
    private void OnOpenCharacterMenu(OpenCharacterMenuEvent ev)
    {
        _ui.GetUIController<CharacterUIController>().OpenWindow();
    }
}
