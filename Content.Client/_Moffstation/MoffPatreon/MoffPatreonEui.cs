using System.Diagnostics;
using Content.Client.Eui;
using Content.Shared._Moffstation.MoffPatreon;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Moffstation.MoffPatreon;

[UsedImplicitly]
public sealed class MoffPatreonEui : BaseEui
{
    private readonly MoffPatreonWindow _window = new();

    public MoffPatreonEui()
    {
        _window.OnRemovePatron += userId => SendMessage(new MoffPatreonRemoveRequest { UserId = userId });
        _window.OnAddOnlineUser += username => SendMessage(new MoffPatreonAddRequest { UsernameOrId = username });
        _window.OnAddById += id => SendMessage(new MoffPatreonAddRequest { UsernameOrId = id });
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
        if (state is MoffPatreonEuiState s)
        {
            _window.UpdateState(s);
        }
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case MoffPatreonRemoveResponse remove:
                _window.ShowRemoveResponse(remove.Success);
                break;
            case MoffPatreonAddResponse add:
                _window.ShowAddResponse(add.Successful, add.Failed);
                break;
        }
    }
}
