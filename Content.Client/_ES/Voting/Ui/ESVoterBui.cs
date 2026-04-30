using System.Diagnostics;
using System.Numerics;
using Content.Shared._ES.Voting.Components;
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

        _window.OnVoteChanged += (entity, option) =>
        {
            var netEnt = EntMan.GetNetEntity(entity);
            EntMan.RaisePredictiveEvent(new ESSetVoteMessage(netEnt, option));
        };
    }

    public override void Update()
    {
        base.Update();

        _window?.Update(Owner);
    }
}
