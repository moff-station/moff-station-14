using System.Linq;
using Content.Server._Moffstation.Preferences;
using Content.Server.Players.JobWhitelist;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Events;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Station;

/// <summary>
/// Sources round-start job candidacy from every one of a player's active characters rather than just
/// the selected one. Uses <see cref="StationJobsGetCandidatesEvent"/> in the opposite direction to
/// JobWhitelistSystem and PlayTimeTrackingSystem, which narrow the same list.
/// </summary>
public sealed class MoffJobCandidateSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly MoffCharacterSelectionManager _selection = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Must run before the two systems that narrow the same list, or the jobs added here would
        // skip their playtime and whitelist filtering.
        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnGetCandidates,
            before: [typeof(PlayTimeTrackingSystem), typeof(JobWhitelistSystem)]);
    }

    private void OnGetCandidates(ref StationJobsGetCandidatesEvent ev)
    {
        var active = GetActiveProfiles(ev.Player);

        // Nothing cached, so leave upstream's seed from the selected character alone.
        if (active.Count == 0)
            return;

        // Replace rather than add to: the selected character contributes nothing if its slot is
        // inactive, and upstream seeded the list from it unconditionally.
        ev.Jobs.Clear();

        foreach (var profile in active)
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

    /// <summary>Every character of <paramref name="player"/> whose slot is active.</summary>
    public List<HumanoidCharacterProfile> GetActiveProfiles(NetUserId player)
    {
        var result = new List<HumanoidCharacterProfile>();

        if (!_prefs.TryGetCachedPreferences(player, out var prefs))
            return result;

        var state = _selection.GetState(player);

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is HumanoidCharacterProfile humanoid && state.IsSlotEnabled(slot))
                result.Add(humanoid);
        }

        return result;
    }
}
