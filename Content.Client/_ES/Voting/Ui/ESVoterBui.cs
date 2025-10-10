using System.Numerics;
using Content.Shared._ES.Voting.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Voting.Ui;

[UsedImplicitly]
public sealed class ESVoterBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESVotingWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESVotingWindow>();
        _window.OpenCenteredAt(new Vector2(0.25f, 0.25f));
        _window.Update(Owner);

        _window.OnVoteChanged += (entity, option) =>
        {
            var netEnt = EntMan.GetNetEntity(entity);
            SendPredictedMessage(new ESSetVoteMessage(netEnt, option));
        };
    }

    public override void Update()
    {
        base.Update();

        _window?.Update(Owner);
    }
}
