using Content.Shared._Moffstation.Silicons.StationAi;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Silicons.StationAi;

[UsedImplicitly]
public sealed class AiShellControllerBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private AiShellSelectionMenu? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<AiShellSelectionMenu>();

        _window.SelectShell += e => SendMessage(new SelectAiShellMessage(EntMan.GetNetEntity(e)));
        _window.JumpToShell += e => SendPredictedMessage(new JumpToAiShellMessage(EntMan.GetNetEntity(e)));
        _window.EnterShell += e => SendPredictedMessage(new StartAiShellControlMessage(EntMan.GetNetEntity(e)));

        if (EntMan.TryGetComponent<AiShellControllerComponent>(Owner, out var component))
        {
            _window?.Populate(
                new AiShellControllerBuiState(
                    EntMan.GetNetEntity(component.SelectedShell),
                    EntMan.GetNetEntityList(component.ControllableShells)
                )
            );
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not AiShellControllerBuiState cast)
            return;

        _window?.Populate(cast);
    }
}
