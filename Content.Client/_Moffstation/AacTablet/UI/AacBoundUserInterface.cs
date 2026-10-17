using Content.Shared._Moffstation.AacTablet;
using Content.Shared._Moffstation.Extensions;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.AacTablet.UI;

[UsedImplicitly]
public sealed partial class AacBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _protoMan = default!;

    [ViewVariables]
    private AacWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AacWindow>();
        _window.PhraseButtonPressed += phraseId => SendMessage(new AacTabletSendPhraseMessage(phraseId));
        _window.SetAacPhrasePacks(
            _protoMan.ResolveAll(EntMan.GetComponentOrNull<AacTabletComponent>(Owner)?.Packs ?? [])
        );
    }
}
