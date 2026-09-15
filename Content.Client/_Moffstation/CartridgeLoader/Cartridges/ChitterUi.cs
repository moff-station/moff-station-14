using Content.Client.UserInterface.Fragments;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.CartridgeLoader.Cartridges;

public sealed partial class ChitterUi : UIFragment
{
    private ChitterUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new ChitterUiFragment();

        _fragment.OnUiMessage += (type, chatId, targetNumber, targetNumbers, content, profilePictureId, chatName) =>
        {
            var message = new ChitterUiMessageEvent(type, chatId, targetNumber, targetNumbers, content, profilePictureId, chatName);

            var wrapper = new CartridgeUiMessage(message);
            userInterface.SendMessage(wrapper);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is ChitterUiState cast)
            _fragment?.UpdateState(cast);
    }
}
