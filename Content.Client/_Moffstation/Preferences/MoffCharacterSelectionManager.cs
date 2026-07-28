using Content.Shared._Moffstation.Preferences;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Preferences;

/// <summary>
/// Client mirror of the server's multi-character selection state. Separate from
/// ClientPreferencesManager so upstream needs no modification. Changes apply optimistically.
/// </summary>
public sealed class MoffCharacterSelectionManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;

    /// <summary>Raised on any state change, from the server or a local edit.</summary>
    public event Action? OnStateChanged;

    public MoffCharacterSelectionState State { get; private set; } = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgMoffCharacterSelectionState>(HandleState);
        _netManager.RegisterNetMessage<MsgUpdateMoffJobPriorities>();
        _netManager.RegisterNetMessage<MsgSetMoffCharacterEnabled>();

        _baseClient.RunLevelChanged += OnRunLevelChanged;
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
            State = new MoffCharacterSelectionState();
    }

    private void HandleState(MsgMoffCharacterSelectionState message)
    {
        State = message.State;
        OnStateChanged?.Invoke();
    }

    public JobPriority GetPriority(ProtoId<JobPrototype> job)
    {
        return State.GetPriority(job);
    }

    public bool IsSlotEnabled(int slot)
    {
        return State.IsSlotEnabled(slot);
    }

    /// <summary>Replaces the priorities and pushes them to the server.</summary>
    public void UpdateJobPriorities(Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
    {
        State.JobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(priorities);
        State.Normalize();

        _netManager.ClientSendMessage(new MsgUpdateMoffJobPriorities
        {
            JobPriorities = State.JobPriorities,
        });

        OnStateChanged?.Invoke();
    }

    /// <summary>Marks a slot active or inactive and pushes it to the server.</summary>
    public void SetCharacterEnabled(int slot, bool enabled)
    {
        State.EnabledSlots[slot] = enabled;

        _netManager.ClientSendMessage(new MsgSetMoffCharacterEnabled
        {
            Slot = slot,
            Enabled = enabled,
        });

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Slots are not reindexed on deletion, so a new character can inherit a deleted one's
    /// disabled flag.
    /// </summary>
    public void ResetSlot(int slot)
    {
        if (!State.EnabledSlots.TryGetValue(slot, out var enabled) || enabled)
            return;

        SetCharacterEnabled(slot, true);
    }
}
