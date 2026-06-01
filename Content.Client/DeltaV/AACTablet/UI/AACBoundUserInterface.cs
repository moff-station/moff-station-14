using Content.Shared.DeltaV.AACTablet;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.DeltaV.AACTablet.UI;

[UsedImplicitly]
public sealed partial class AACBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AACWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AACWindow>();
        _window.PhraseButtonPressed += phraseId => SendMessage(new AACTabletSendPhraseMessage(phraseId));
    }
}
