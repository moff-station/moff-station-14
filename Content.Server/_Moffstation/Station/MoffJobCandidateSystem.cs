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
        foreach (var profile in GetActiveProfiles(ev.Player))
        {
            foreach (var job in profile.JobPriorities.Keys)
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
        return GetActiveProfiles(player)
            .Where(profile => profile.JobPriorities.ContainsKey(job))
            .ToList();
    }

    /// <summary>
    /// The jobs any active character of <paramref name="player"/> will take, at the player-global
    /// priority. <paramref name="fallback"/> covers guests, who have no stored priorities.
    /// </summary>
    public Dictionary<ProtoId<JobPrototype>, JobPriority> GetJobPriorities(
        NetUserId player,
        HumanoidCharacterProfile fallback)
    {
        var result = new Dictionary<ProtoId<JobPrototype>, JobPriority>();

        foreach (var profile in GetActiveProfiles(player))
        {
            foreach (var job in profile.JobPriorities.Keys)
            {
                if (result.ContainsKey(job))
                    continue;

                var priority = _selection.GetEffectivePriority(player, job, fallback);

                if (priority != JobPriority.Never)
                    result.Add(job, priority);
            }
        }

        return result;
    }

    private IEnumerable<HumanoidCharacterProfile> GetActiveProfiles(NetUserId player)
    {
        if (!_prefs.TryGetCachedPreferences(player, out var prefs))
            yield break;

        var state = _selection.GetState(player);

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is HumanoidCharacterProfile humanoid && state.IsSlotEnabled(slot))
                yield return humanoid;
        }
    }
}
