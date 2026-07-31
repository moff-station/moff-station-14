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
public sealed partial class MoffCharacterSelectionManager
{
    [Dependency] private IClientNetManager _netManager = default!;
    [Dependency] private IBaseClient _baseClient = default!;

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

    /// <summary>
    /// The job <paramref name="profile"/> is most likely to be assigned: of the jobs it will take,
    /// whichever the player rates highest. Null only if it will take no job at all.
    /// </summary>
    /// <remarks>
    /// A character that has selected only jobs the player rates <see cref="JobPriority.Never"/> still
    /// reports one of them, so it previews and lists as a job it actually chose rather than as a
    /// passenger. Ties are broken by job id so the answer is stable between sessions.
    /// </remarks>
    public ProtoId<JobPrototype>? GetPreferredJob(HumanoidCharacterProfile profile)
    {
        ProtoId<JobPrototype>? best = null;
        var bestPriority = JobPriority.Never;

        foreach (var job in profile.JobPriorities.Keys)
        {
            var priority = GetPriority(job);

            if (best != null
                && (priority < bestPriority
                    || priority == bestPriority
                    && string.CompareOrdinal(job.Id, best.Value.Id) >= 0))
            {
                continue;
            }

            best = job;
            bestPriority = priority;
        }

        return best;
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
