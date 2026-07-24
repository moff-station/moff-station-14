using System.Numerics;
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
        _window.OpenCenteredAt(_defaultLocation); // Moffstation - off center the window, I like it better
        _window.Update(Owner);
    }

    public override void Update()
    {
        base.Update();

        _window?.Update(Owner);
    }
}
