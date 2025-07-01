using Content.Client.Weapons.Melee.UI;
using Content.Shared._Moffstation.Paper.Components;
using Content.Shared.Speech.Components;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Paper.UI;

/// <summary>
/// Initializes a <see cref="ForgeSignatureWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class ForgeSignatureBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ForgeSignatureWindow? _window;

    public ForgeSignatureBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ForgeSignatureWindow>();
        _window.OnSignatureEntered += OnSignatureChanged;
    }

    private void OnSignatureChanged(string newBattlecry)
    {
        SendMessage(new SignatureChangedMessage(newBattlecry));
    }

    /// <summary>
    /// Update the UI state based on server-sent info
    /// </summary>
    /// <param name="state"></param>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not ForgeSignatureBoundUserInterfaceState cast)
            return;

        _window.SetCurrentSignature(cast.CurrentSignature);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}
