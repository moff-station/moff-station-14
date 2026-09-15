using Content.Client._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Chitter;

// Thin shell around the same ChitterUiFragment control the PDA cartridge uses, hosted in a plain window instead of
// a cartridge loader - see ChitterUi.cs for the cartridge-hosted equivalent this mirrors.
[UsedImplicitly]
public sealed class ChitterAiBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ChitterAiWindow? _window;
    private ChitterUiFragment? _fragment;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChitterAiWindow>();
        _fragment = new ChitterUiFragment();
        _window.Contents.AddChild(_fragment);
        _fragment.HorizontalExpand = true;
        _fragment.VerticalExpand = true;

        _fragment.OnUiMessage += (type, chatId, targetNumber, targetNumbers, content, profilePictureId, chatName) =>
        {
            SendMessage(new ChitterAiUiMessageEvent(type, chatId, targetNumber, targetNumbers, content, profilePictureId, chatName));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ChitterUiState s)
            _fragment?.UpdateState(s);
    }
}
