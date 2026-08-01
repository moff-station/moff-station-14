using Content.Shared._Moffstation.AacTablet;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.AacTablet.UI;

[UsedImplicitly]
public sealed partial class AacBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AacWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AacWindow>();
        _window.PhraseButtonPressed += phraseId => SendMessage(new AacTabletSendPhraseMessage(phraseId));
    }
}
