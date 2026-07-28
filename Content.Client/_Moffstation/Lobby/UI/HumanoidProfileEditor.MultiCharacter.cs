using Content.Client._Moffstation.Lobby.UI;

// Partial of the upstream HumanoidProfileEditor, so it must sit in the upstream namespace.
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private MoffJobPriorityWindow? _moffJobPriorityWindow;

    private void InitializeMoffMultiCharacter()
    {
        MoffJobPriorityButton.OnPressed += _ => OpenMoffJobPriorityWindow();
    }

    private void OpenMoffJobPriorityWindow()
    {
        if (_moffJobPriorityWindow is { Disposed: false })
        {
            _moffJobPriorityWindow.Close();
            _moffJobPriorityWindow = null;
            return;
        }

        _moffJobPriorityWindow = new MoffJobPriorityWindow();
        _moffJobPriorityWindow.OnClose += () => _moffJobPriorityWindow = null;
        _moffJobPriorityWindow.OpenCentered();
    }

    private void ShutdownMoffMultiCharacter()
    {
        _moffJobPriorityWindow?.Close();
        _moffJobPriorityWindow = null;
    }
}
