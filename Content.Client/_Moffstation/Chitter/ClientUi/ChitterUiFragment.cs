using Content.Client.UserInterface.Fragments;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.CartridgeLoader;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._Moffstation.Chitter.ClientUi;

[UsedImplicitly] // Referenced in YAML
public sealed partial class ChitterUiFragment : UIFragment
{
    private ChitterClientControl? _inner;

    public override Control GetUIFragmentRoot()
    {
        DebugTools.AssertNotNull(_inner);
        return _inner!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _inner = new ChitterClientControl();
        _inner.HandleEventsWithBuiMessages(
            msg => userInterface.SendPredictedMessage(new CartridgeUiMessage(new ChitterCartridgeMessageEvent(msg))),
            msg => userInterface.SendMessage(new CartridgeUiMessage(new ChitterCartridgeMessageEvent(msg)))
        );
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is ChitterUiState cast)
            _inner?.Update(cast);
    }
}
