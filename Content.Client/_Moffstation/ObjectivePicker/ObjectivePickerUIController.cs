using Content.Client._DV.CustomObjectiveSummary;
using Content.Shared._DV.CustomObjectiveSummary;
using Content.Shared._Moffstation.Objectives;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;

namespace Content.Client._Moffstation.ObjectivePicker;

public sealed class ObjectivePickerUIController : UIController
{
    [Dependency] private readonly IClientNetManager _net = default!;

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
    }
}
