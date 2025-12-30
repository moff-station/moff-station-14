using Content.Shared._Moffstation.Objectives;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Moffstation.ObjectivePicker;

public sealed class ObjectivePickerUIController : UIController
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private ObjectivePickerWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ObjectivePickerOpenMessage>(OnCustomObjectiveSummaryOpen);
    }

    private void OnCustomObjectiveSummaryOpen(ObjectivePickerOpenMessage msg, EntitySessionEventArgs args)
    {
        OpenWindow();
    }

    public void OpenWindow()
    {
        // If a window is already open, close it
        _window?.Close();

        _window = new ObjectivePickerWindow();
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
        _window.OnSelected += OnSelected;
        _window.OnSubmitted += OnSubmitted;
    }

    private void OnSelected(NetEntity netEntity)
    {
        if (!_window!.SelectedObjectives.Remove(netEntity))
            _window.SelectedObjectives.Add(netEntity);
        _window.UpdateState();
    }

    private void OnSubmitted(HashSet<NetEntity> selectedObjectives, NetEntity mindId)
    {
        var message = new ObjectivePickerSelected
        {
            MindId = mindId,
            SelectedObjectives = selectedObjectives,
        };
        _net.SendSystemNetworkMessage(message);
        _window?.Close();
    }
}
