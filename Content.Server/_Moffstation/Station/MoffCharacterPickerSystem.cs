using System.Linq;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Station;

/// <summary>
/// Picks which of a player's active characters spawns, once a job has been assigned to them.
/// </summary>
public sealed partial class MoffCharacterPickerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MoffJobCandidateSystem _candidates = default!;
    [Dependency] private PlayTimeTrackingSystem _playTime = default!;

    /// <summary>
    /// So antag loadouts equip the character that spawned, not the one selected in the lobby.
    /// </summary>
    private readonly Dictionary<NetUserId, HumanoidCharacterProfile> _spawnedProfiles = new();

    /// <summary>
    /// A late join names the character it wants, so nothing should pick one at random for it.
    /// </summary>
    private readonly Dictionary<NetUserId, HumanoidCharacterProfile> _explicitChoices = new();

    [SubscribeLocalEvent]
    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _spawnedProfiles.Clear();
        _explicitChoices.Clear();
    }

    /// <summary>Records the character a late join asked to spawn as.</summary>
    public void SetExplicitChoice(NetUserId player, HumanoidCharacterProfile profile)
    {
        _explicitChoices[player] = profile;
    }

    /// <summary>
    /// Returns and consumes a pending explicit choice, so it applies to exactly one spawn.
    /// </summary>
    public HumanoidCharacterProfile? TakeExplicitChoice(NetUserId player)
    {
        if (!_explicitChoices.Remove(player, out var profile))
            return null;

        _spawnedProfiles[player] = profile;
        return profile;
    }

    /// <summary>
    /// Picks a character for a player given a specific job.
    /// </summary>
    public HumanoidCharacterProfile? PickProfile(ICommonSession player, ProtoId<JobPrototype> job)
    {
        var eligible = _candidates.GetEligibleProfiles(player.UserId, job);

        if (eligible.Count == 0)
            return null;

        var allowed = eligible.Where(profile => _playTime.IsAllowed(player, job, profile)).ToList();

        if (allowed.Count == 0)
        {
            return null;
        }

        var picked = _random.Pick(allowed);
        _spawnedProfiles[player.UserId] = picked;

        return picked;
    }

    /// <summary>For picking a job when the caller has not assigned one, e.g. late joins.</summary>
    public Dictionary<ProtoId<JobPrototype>, JobPriority> GetJobPriorities(
        NetUserId player,
        HumanoidCharacterProfile fallback)
    {
        return _candidates.GetJobPriorities(player, fallback);
    }

    /// <summary>Null if they have not spawned this round.</summary>
    public HumanoidCharacterProfile? GetSpawnedProfile(NetUserId player)
    {
        return _spawnedProfiles.GetValueOrDefault(player);
    }
}
