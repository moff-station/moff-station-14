using Content.Shared._Moffstation.Paper.Components;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client._Moffstation.Paper.UI;

/// <summary>
/// Initializes a <see cref="ForgeSignatureWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class ForgeSignatureBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;

    [ViewVariables]
    private ForgeSignatureWindow? _window;

    protected override void Open()
    {
        base.Open();

        if (!_entManager.HasComponent<ForgeSignatureComponent>(Owner))
            return;
            
        _window = this.CreateWindow<ForgeSignatureWindow>();
        _window.SetMaxSignatureLength(_cfgManager.GetCVar(CCVars.MaxNameLength));

        _window.OnSignatureChanged += OnSignatureChanged;
        Reload();
    }

    private void OnSignatureChanged(string newSignature)
    {
        if (!_entManager.TryGetComponent<ForgeSignatureComponent>(Owner, out var pen) ||
            pen.Signature.Equals(newSignature))
            return;

        pen.Signature = newSignature;
        SendPredictedMessage(new ForgedSignatureChangedMessage(newSignature));
    }

    public void Reload()
    {
        if (_window == null || !_entManager.TryGetComponent(Owner, out ForgeSignatureComponent? component))
            return;

        _window.SetCurrentSignature(component.Signature);
    }
}
