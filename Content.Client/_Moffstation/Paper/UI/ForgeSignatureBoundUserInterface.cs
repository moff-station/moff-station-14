using Content.Shared._Moffstation.Paper.Components;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client._Moffstation.Paper.UI;

/// <summary>
/// Initializes a <see cref="ForgeSignatureWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class ForgeSignatureBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;

    [ViewVariables]
    private ForgeSignatureWindow? _window;

    public ForgeSignatureBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ForgeSignatureWindow>();

        if (_entManager.TryGetComponent(Owner, out ForgeSignatureComponent? labeler))
        {
            _window.SetMaxSignatureLength(_cfgManager.GetCVar(CCVars.MaxNameLength));
        }

        _window.OnSignatureChanged += OnSignatureChanged;
        Reload();
    }

    private void OnSignatureChanged(string newSignature)
    {
        if (_entManager.TryGetComponent(Owner, out ForgeSignatureComponent? pen) &&
            pen.Signature.Equals(newSignature))
            return;

        if (pen is not { } penComp)
            return;
        penComp.Signature = newSignature;
        SendPredictedMessage(new ForgedSignatureChangedMessage(newSignature));
    }

    public void Reload()
    {
        if (_window == null || !_entManager.TryGetComponent(Owner, out ForgeSignatureComponent? component))
            return;

        _window.SetCurrentSignature(component.Signature);
    }
}
