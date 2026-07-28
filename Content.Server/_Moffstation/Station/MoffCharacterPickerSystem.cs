using System.Linq;
using Content.Server.Antag;
using Content.Shared.GameTicking;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
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
    [Dependency] private readonly IPlayerManager _playerManager = default!;
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
    /// Falls back to <paramref name="fallback"/> when no active character qualifies.
    /// </summary>
    public HumanoidCharacterProfile PickProfile(
        NetUserId player,
        ProtoId<JobPrototype> job,
        HumanoidCharacterProfile fallback)
    {
        var picked = PickProfileOrNull(player, job) ?? fallback;

        _spawnedProfiles[player] = picked;

        return picked;
    }

    /// <summary>
    /// Null if they were not spawned by the round-start assigner.
    /// </summary>
    public HumanoidCharacterProfile? GetSpawnedProfile(NetUserId player)
    {
        return _spawnedProfiles.GetValueOrDefault(player);
    }

    private HumanoidCharacterProfile? PickProfileOrNull(NetUserId player, ProtoId<JobPrototype> job)
    {
        var eligible = _candidates.GetEligibleProfiles(player, job);

        if (eligible.Count == 0)
            return null;

        if (!_protoManager.TryIndex(job, out var jobProto))
            return null;

        if (!_playerManager.TryGetSessionById(player, out var session))
            return null;

        var playTimes = _playTime.GetPlayTimes(session);

        // Drop characters that don't meet the job's own requirements, e.g. age or species.
        var filtered = eligible.Where(profile =>
            JobRequirements.TryRequirementsMet(
                jobProto,
                playTimes,
                out _,
                EntityManager,
                _protoManager,
                profile));

        // If the player has already been preselected for antags, only characters that opted in to
        // every one of those antags can fill the slot.
        foreach (var antagSet in _antag.GetMoffPreSelectedAntagPrefRoles(session))
        {
            filtered = filtered.Where(profile => antagSet.Overlaps(profile.AntagPreferences));
        }

        var final = filtered.ToList();

        return final.Count == 0 ? null : _random.Pick(final);
    }
}
