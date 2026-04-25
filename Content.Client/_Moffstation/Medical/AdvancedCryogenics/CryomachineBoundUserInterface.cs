using Content.Shared._Moffstation.Medical.AdvancedCryogenics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Medical.AdvancedCryogenics;

[UsedImplicitly]
public sealed class CryomachineBoundUserInterface : BoundUserInterface
{
    private CryomachineWindow? _window;

    public CryomachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCenteredLeft<CryomachineWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.OnJumpStartBrainPressed += JumpStartBrainPressed;
        _window.OnDetachCapsulePressed += DetachCapsulePressed;
        _window.OnEjectBeakerPressed += EjectBeakerPressed;
    }

    private void JumpStartBrainPressed()
    {
        SendMessage(new CryomachineSimpleUiMessage(CryomachineSimpleUiMessage.MessageType.JumpstartBrain));
    }

    private void DetachCapsulePressed()
    {
        SendMessage(new CryomachineSimpleUiMessage(CryomachineSimpleUiMessage.MessageType.DetachCapsule));
    }

    private void EjectBeakerPressed()
    {
        SendMessage(new CryomachineSimpleUiMessage(CryomachineSimpleUiMessage.MessageType.EjectBeaker));
    }
}
