using System.Linq;
using Content.Server.Antag;
using Content.Shared.GameTicking;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Station;

/// <summary>
/// Picks which of a player's active characters spawns, once a job has been assigned to them.
/// Ported from upstream PR #36493.
/// </summary>
public sealed class MoffCharacterPickerSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MoffJobCandidateSystem _candidates = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playTime = default!;

    /// <summary>
    /// So antag loadouts equip the character that spawned, not the one selected in the lobby.
    /// </summary>
    private readonly Dictionary<NetUserId, HumanoidCharacterProfile> _spawnedProfiles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _spawnedProfiles.Clear();
    }

    /// <summary>
    /// Null when no active character can take <paramref name="job"/>; never falls back to the
    /// character selected in the lobby.
    /// </summary>
    public HumanoidCharacterProfile? PickProfile(ICommonSession player, ProtoId<JobPrototype> job)
    {
        var picked = PickProfileOrNull(player, job);

        if (picked != null)
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

    private HumanoidCharacterProfile? PickProfileOrNull(ICommonSession player, ProtoId<JobPrototype> job)
    {
        var eligible = _candidates.GetEligibleProfiles(player.UserId, job);

        if (eligible.Count == 0)
            return null;

        if (!_protoManager.TryIndex(job, out var jobProto))
            return null;

        var playTimes = _playTime.GetPlayTimes(player);

        // Drop characters that don't meet the job's own requirements, e.g. age or species.
        var filtered = eligible.Where(profile =>
            JobRequirements.TryRequirementsMet(
                jobProto,
                playTimes,
                out _,
                EntityManager,
                _protoManager,
                profile));

        // A preselected antag can only be filled by a character that opted in to it.
        foreach (var antagSet in _antag.GetMoffPreSelectedAntagPrefRoles(player))
        {
            filtered = filtered.Where(profile => antagSet.Overlaps(profile.AntagPreferences));
        }

        var final = filtered.ToList();

        return final.Count == 0 ? null : _random.Pick(final);
    }
}
