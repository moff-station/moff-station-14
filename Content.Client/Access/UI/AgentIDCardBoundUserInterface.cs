using Content.Shared._CD.NanoChat;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.StatusIcon;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Access.UI;

/// <summary>
/// Initializes a <see cref="AgentIDCardWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class AgentIDCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AgentIDCardWindow? _window;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out AgentIDCardComponent? agent))
            return;

        _window = this.CreateWindow<AgentIDCardWindow>();

        _window.OnNameChanged += OnNameChanged;
        _window.OnJobChanged += OnJobChanged;
        _window.OnJobIconChanged += OnJobIconChanged;
        _window.OnNumberChanged += OnNumberChanged; // CD

        ProtoId<JobIconPrototype> currentIcon = default;
        if (EntMan.TryGetComponent<IdCardComponent>(Owner, out var card))
            currentIcon = card.JobIcon;

        _window.SetAllowedIcons(agent.IconGroups, currentIcon);
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_window == null)
            return;

        if (!EntMan.TryGetComponent<IdCardComponent>(Owner, out var card))
            return;

        EntMan.TryGetComponent<NanoChatCardComponent>(Owner, out var nanochat); // Moff - Nanochat

        _window.Update(card, nanochat); // Moff - Nanochat
    }

    // CD - Add number change handlerz
    private void OnNumberChanged(uint newNumber)
    {
        SendPredictedMessage(new AgentIDCardNumberChangedMessage(newNumber));
    }

    private void OnNameChanged(string newName)
    {
        SendPredictedMessage(new AgentIDCardNameChangedMessage(newName));
    }

    private void OnJobChanged(string newJob)
    {
        SendPredictedMessage(new AgentIDCardJobChangedMessage(newJob));
    }

    private void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
    {
        SendPredictedMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
    }
}
