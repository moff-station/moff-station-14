using Content.Shared._Moffstation.GaussFabricator;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.GaussFabricator;

[UsedImplicitly]
public sealed class GaussFabricatorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GaussFabricatorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GaussFabricatorWindow>();
        _window.OnAdjustDrawRate += delta => SendPredictedMessage(new GaussFabricatorAdjustDrawRateMessage(delta));
        _window.OnToggle += on => SendPredictedMessage(new GaussFabricatorToggleMessage(on));
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window != null && EntMan.TryGetComponent<GaussFabricatorComponent>(Owner, out var comp))
            _window.UpdateSettings(comp);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is GaussFabricatorBuiState cast)
            _window?.UpdateState(cast);
    }
}
