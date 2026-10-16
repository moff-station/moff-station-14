using Content.Shared._Moffstation.Chitter;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Chitter.ClientUi;

[UsedImplicitly]
public sealed class IntrinsicChitterClientBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private ChitterClientWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChitterClientWindow>();
        _window.Inner.HandleEventsWithBuiMessages(SendPredictedMessage, SendMessage);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ChitterUiState s)
            _window?.Inner.Update(s);
    }
}
