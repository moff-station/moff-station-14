using System.Linq;
using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Shared._Moffstation.Objectives;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Random;

namespace Content.Client._Moffstation.ObjectivePicker;

[UsedImplicitly]
public sealed partial class ObjectivePickerUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private IEntityNetworkManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;

    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;    // Moffstation - Character Menu Redesign

    private ObjectivePickerWindow? _window;

    public void OnStateExited(GameplayState state)
    {
        if (_window == null)
            return;

        _window.Close();
        _window = null;
    }

    public void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<ObjectivePickerWindow>();
        _window.OpenCentered();
        _window.OnClose += () => _window = null;
        _window.OnSelectedChange += OnSelectedChange;
        _window.OnSubmitted += OnSubmitted;
        _window.OnRandomize += OnRandomize;
        _window.OnClear += OnClear;
    }

    private void OnSelectedChange(NetEntity netEntity)
    {
        if (_window == null)
            return;

        if (!_window.SelectedObjectives.Remove(netEntity))
            _window.SelectedObjectives.Add(netEntity);
        _window.UpdateState();
    }

    private void OnSubmitted(HashSet<NetEntity> selectedObjectives, NetEntity mindId)
    {
        if (_window == null)
            return;

        var message = new ObjectivePickerSelected
        {
            MindId = mindId,
            SelectedObjectives = selectedObjectives,
        };
        _net.SendSystemNetworkMessage(message);
        _window.Close();
        // Moffstation - Start - Character Menu Redesign
        _characterInfo.RequestCharacterInfo();
        // Moffstation - End
    }

    private void OnRandomize(HashSet<NetEntity> objectiveList, int pickCount)
    {
        if (_window == null)
            return;

        _window.SelectedObjectives.Clear();

        foreach (var _ in Enumerable.Range(0, pickCount))
        {
            _window.SelectedObjectives.Add(_random.Pick(objectiveList));
        }
        _window.UpdateState();
    }

    private void OnClear()
    {
        if (_window == null)
            return;

        _window.SelectedObjectives.Clear();
        _window.UpdateState();
    }
}
