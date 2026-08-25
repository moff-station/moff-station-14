using System.Numerics;
using Content.Shared._Moffstation.Voting.Components; // Moff
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Voting.Ui;

[UsedImplicitly]
public sealed class ESVoterBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESVotingWindow? _window;
    private readonly Vector2 _defaultLocation = new (0.1f, 0.3f);

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESVotingWindow>();
        // Moff Start - off center the window, I like it better, and send the enroll messages from here
        _window.OpenCenteredAt(_defaultLocation);
        SetupBuiMessages();
        // Moff end
        _window.Update(Owner);
    }

    // Moff start - Our Bui messages, separated here for cleanliness
    private void SetupBuiMessages()
    {
        _window!.OnSetEnroll += (enroller, enrolled) =>
            SendMessage(new MoffSetEnrollMessage(EntMan.GetNetEntity(enroller), enrolled));
        _window.OnSetRandom += (enroller, random) =>
            SendMessage(new MoffSetEnrollRandomMessage(EntMan.GetNetEntity(enroller), random));
        _window.OnEarlyStartEnroll += enrollEvent =>
            SendMessage(new MoffEarlyStartEnrollRequest(EntMan.GetNetEntity(enrollEvent)));
    }
    // Moff end

    public override void Update()
    {
        base.Update();

        _window?.Update(Owner);
    }
}
