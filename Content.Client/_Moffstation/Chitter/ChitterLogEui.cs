using Content.Client.Eui;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Moffstation.Chitter;

[UsedImplicitly]
public sealed class ChitterLogEui : BaseEui
{
    private readonly ChitterLogWindow _window = new();

    public ChitterLogEui()
    {
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is ChitterLogEuiState s)
            _window.UpdateState(s);
    }
}
