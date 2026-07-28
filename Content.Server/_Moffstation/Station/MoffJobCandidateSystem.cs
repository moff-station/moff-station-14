using System.Linq;
using Content.Server._Moffstation.Preferences;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Events;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Station;

/// <summary>
/// Widens round-start job candidacy to every one of a player's active characters. Uses
/// <see cref="StationJobsGetCandidatesEvent"/> in the opposite direction to JobWhitelistSystem and
/// PlayTimeTrackingSystem, which narrow the same list.
/// </summary>
public sealed class MoffJobCandidateSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly MoffCharacterSelectionManager _selection = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnGetCandidates);
    }

    private void OnGetCandidates(ref StationJobsGetCandidatesEvent ev)
    {
        if (!_prefs.TryGetCachedPreferences(ev.Player, out var prefs))
            return;

        var state = _selection.GetState(ev.Player);

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is not HumanoidCharacterProfile humanoid)
                continue;

            if (!state.IsSlotEnabled(slot))
                continue;

            foreach (var job in humanoid.JobPriorities.Keys)
            {
                if (!ev.Jobs.Contains(job))
                    ev.Jobs.Add(job);
            }
        }
    }

    /// <summary>
    /// Every active character of <paramref name="player"/> willing to take <paramref name="job"/>.
    /// </summary>
    public List<HumanoidCharacterProfile> GetEligibleProfiles(NetUserId player, ProtoId<JobPrototype> job)
    {
        var result = new List<HumanoidCharacterProfile>();

        if (!_prefs.TryGetCachedPreferences(player, out var prefs))
            return result;

        var state = _selection.GetState(player);

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is not HumanoidCharacterProfile humanoid)
                continue;

            if (!state.IsSlotEnabled(slot))
                continue;

            if (!humanoid.JobPriorities.ContainsKey(job))
                continue;

            result.Add(humanoid);
        }

        return result;
    }
}
