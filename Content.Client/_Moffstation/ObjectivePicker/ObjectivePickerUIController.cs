using Content.Shared._Moffstation.Objectives;
using Content.Shared.Mind;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;

namespace Content.Client._Moffstation.ObjectivePicker;

public sealed class ObjectivePickerUIController : UIController
{
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    // [Dependency] private readonly SharedMindSystem _mind = default!;

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
    }

    private void OnSelected(NetEntity netEntity)
    {
        if (_window!.SelectedObjectives.Remove(netEntity))
            _window.SelectedObjectives.Add(netEntity);
    }

    private void OnSubmitted(HashSet<NetEntity> selectedObjectives, EntityUid mindId)
    {
        var message = new ObjectivePickerSelected
        {
            UserId = _entity.GetNetEntity(mindId),
            SelectedObjectives = selectedObjectives,
        };
        _entity.EntityNetManager.SendSystemNetworkMessage(message);
    }
}
