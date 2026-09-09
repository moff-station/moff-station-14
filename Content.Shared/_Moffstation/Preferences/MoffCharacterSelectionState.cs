using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Preferences;

/// <summary>
/// Player-global multi-character selection state. A character records which jobs it will take (the
/// keys of <see cref="HumanoidCharacterProfile.JobPriorities"/>); the priority applied to them lives
/// here and is shared across all of the player's characters.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct MoffCharacterSelectionState
{
    /// <summary>
    /// Jobs absent from this dictionary count as <see cref="JobPriority.Never"/>.
    /// </summary>
    public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities { get; init; } = new();

    /// <summary>
    /// Slots absent from this set count as enabled, so characters predating multi-character
    /// selection keep working.
    /// </summary>
    public Dictionary<int, bool> EnabledSlots { get; init; } = new();

    /// <summary>
    /// Whether this came from the database. Plain guests have nowhere to persist priorities and get
    /// a blank non-authoritative state; consumers must fall back to per-character priorities for
    /// them, or they would be eligible for no jobs at all.
    /// </summary>
    public bool IsAuthoritative { get; init; }

    public MoffCharacterSelectionState()
    {
    }

    public MoffCharacterSelectionState DeepCopy()
    {
        return this with
        {
            JobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(JobPriorities),
            EnabledSlots = new Dictionary<int, bool>(EnabledSlots),
        };
    }

    public bool IsSlotEnabled(int slot)
    {
        return !EnabledSlots.TryGetValue(slot, out var enabled) || enabled;
    }

    public JobPriority GetPriority(ProtoId<JobPrototype> job)
    {
        return JobPriorities.GetValueOrDefault(job, JobPriority.Never);
    }

    /// <summary>
    /// Drops explicit Never entries and enforces the single-High rule.
    /// </summary>
    public MoffCharacterSelectionState Normalize()
    {
        var normalized = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
        var seenHigh = false;

        foreach (var (job, priority) in JobPriorities)
        {
            if (priority == JobPriority.Never)
                continue;

            if (priority == JobPriority.High)
            {
                if (seenHigh)
                {
                    normalized[job] = JobPriority.Medium;
                    continue;
                }

                seenHigh = true;
            }

            normalized[job] = priority;
        }

        return this with { JobPriorities = normalized };
    }
}
