using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.LogProbe;

public sealed class LogProbeBoundUserInterface : BoundUserInterface
{
    private LogProbeWindow? _window;

    public LogProbeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LogProbeWindow>();
        _window.Title = "Log Probe";

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
        SendMessage(new Shared._Moffstation.LogProbe.LogProbePrintMessage());
    }
}
