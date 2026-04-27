using Content.Shared._Moffstation.Medical.CryoCapsule;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Medical.CryoCapsule;

[UsedImplicitly]
public sealed class CryoLifeSupportBoundUserInterface : BoundUserInterface
{
    private CryoLifeSupportWindow? _window;

    public CryoLifeSupportBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CryoLifeSupportWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.OnJumpStartBrainPressed += ReviveBrainPressed;
        _window.OnDetachCapsulePressed += EjectCapsulePressed;
        _window.OnEjectBeakerPressed += EjectBeakerPressed;
    }

    private void ReviveBrainPressed()
    {
        SendMessage(new CryoLifeSupportSimpleUiMessage(CryoLifeSupportSimpleUiMessage.ActionType.ReviveBrain));
    }

    private void EjectCapsulePressed()
    {
        SendMessage(new CryoLifeSupportSimpleUiMessage(CryoLifeSupportSimpleUiMessage.ActionType.EjectCapsule));
    }

    private void EjectBeakerPressed()
    {
        SendMessage(new CryoLifeSupportSimpleUiMessage(CryoLifeSupportSimpleUiMessage.ActionType.EjectBeaker));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not CryoLifeSupportUiState cast)
            return;

        base.ReceiveMessage(message);
        _window?.SetState(cast);
    }
}
