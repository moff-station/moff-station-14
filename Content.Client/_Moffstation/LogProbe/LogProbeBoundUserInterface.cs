using Content.Shared.CartridgeLoader.Cartridges;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.LogProbe;

[UsedImplicitly]
public sealed class LogProbeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private LogProbeWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LogProbeWindow>();
        _window.Title = Loc.GetString("log-probe-window-title");

        _window.OnPrintPressed += OnPrintPressed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is LogProbeUiState cast)
            _window?.UpdateState(cast);
    }

    private void OnPrintPressed()
    {
        SendMessage(new Shared._Moffstation.LogProbe.LogProbePrintBuiMessage());
    }
}
