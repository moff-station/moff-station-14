using System.Linq;
using Content.Server._Moffstation.Preferences;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Station.Systems;

/// <summary>
/// A collection ("pool") of <see cref="NetUserId"/>s (called "Candidates") and their job preferences organized to make
/// <see cref="StationJobsSystem.AssignJobs">picking candidates for roundstart jobs</see> easier.
/// Add candidates with <see cref="SetCandidates"/>, pick candidates with <see cref="Pick"/> and its variants.
/// </summary>
/// <param name="random">The RNG to use when picking candidates. The RNG is used to choose between two candidates which
/// are otherwise indistinguishable to the pool.</param>
/// <param name="isUserAllowedJob">
/// A predicate which is used to determine if the given user and character is allowed to be assigned the given job. This
/// check is always honored, regardless of picking method. It should check things like job bans or playtime restrictions.
/// </param>
/// <param name="sameDepartmentJobs">
/// This function is used to retrieve alternate jobs allowed when using <see cref="PickSameDepartmentCandidate"/>.
/// Although the name specifically mentions "department", this function could be used to return any alternate jobs.
/// </param>
public sealed partial class RoundstartJobCandidates(
    IRobustRandom random,
    Predicate<(NetUserId, HumanoidCharacterProfile, ProtoId<JobPrototype>)> isUserAllowedJob,
    Func<ProtoId<JobPrototype>, IEnumerable<ProtoId<JobPrototype>>> sameDepartmentJobs
)
{
    /// This constructor just instantiates and initializes this object. It's literally equivalent to calling the default
    /// constructor and then calling <see cref="SetCandidates"/>. See those for documentation of the parameters.
    /// <seealso cref="RoundstartJobCandidates"/>
    /// <seealso cref="SetCandidates"/>
    public RoundstartJobCandidates(
        IRobustRandom random,
        Predicate<(NetUserId, HumanoidCharacterProfile, ProtoId<JobPrototype>)> isUserAllowedJob,
        Func<ProtoId<JobPrototype>, IEnumerable<ProtoId<JobPrototype>>> sameDepartmentJobs,
        IEnumerable<(NetUserId, HashSet<HumanoidCharacterProfile>)> profiles,
        Func<NetUserId, IEnumerable<ProtoId<JobPrototype>>, IEnumerable<ProtoId<JobPrototype>>> filterAllowedJobs,
        Func<NetUserId, ProtoId<JobPrototype>, HumanoidCharacterProfile, JobPriority>
            getEffectivePriorityForMoffMultiCharacterSelection
    ) : this(random, isUserAllowedJob, sameDepartmentJobs)
    {
        SetCandidates(profiles, filterAllowedJobs, getEffectivePriorityForMoffMultiCharacterSelection);
    }

    /// The basic user-to-profiles collection of candidates. Used to pick candidates without caring about priorities, etc.
    private readonly Dictionary<NetUserId, HashSet<HumanoidCharacterProfile>> _candidates = new();

    public IReadOnlyDictionary<NetUserId, HashSet<HumanoidCharacterProfile>> Candidates => _candidates;

    /// A collection of users+characters keyed by job and priority. Used to select player jobs by their requested
    /// priorities.
    private readonly Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<PlayerCharacter>>>
        _candidatesByJobAndPriority = new();

    public bool IsEmpty() => _candidates.Count == 0 &&
                             _candidatesByJobAndPriority.Values.Sum(usersByPriority =>
                                 usersByPriority.Values.Sum(users => users.Count)) == 0;

    /// Removes a candidate from this pool, meaning it cannot be selected by <see cref="PickCandidate"/> or similar
    /// functions.
    public bool Remove(NetUserId candidate)
    {
        // A candidate could be in one but not both collection is a "you can take this job" predicate failed.
        var r1 = _candidates.Remove(candidate);
        var r2 = false;
        foreach (var usersByPriority in _candidatesByJobAndPriority.Values)
        {
            foreach (var users in usersByPriority.Values)
            {
                r2 |= users.RemoveWhere(it => it.Player == candidate) > 0;
            }
        }

        return r1 || r2;
    }

    /// Picks a candidate from this pool for <paramref name="job"/> at <paramref name="priority"/>.
    public PlayerCharacter? PickCandidate(ProtoId<JobPrototype> job, JobPriority priority)
    {
        // TODO Maybe "count down" from priorities so that people with higher priorities are considered.
        //  Right now, the assumption is that those people would've been picked already, were they to exist.
        if (_candidatesByJobAndPriority.TryGetValue(job, out var candidates) &&
            candidates.TryGetValue(priority, out var players)
            && players.Count != 0)
        {
            return random.Pick(players);
        }

        return null;
    }

    /// Picks a candidate from this pool for <paramref name="job"/> at <paramref name="priority"/>. The candidates
    /// considered include users who have enabled any job that is returned by <see cref="sameDepartmentJobs"/> when
    /// given <paramref name="job"/>.
    public PlayerCharacter? PickSameDepartmentCandidate(ProtoId<JobPrototype> job, JobPriority priority)
    {
        var jobsInSameDept = sameDepartmentJobs(job);
        var matchingPlayerCharacters = _candidates
            .SelectMany(it => it.Value.Select(it2 => new PlayerCharacter(it.Key, it2)))
            .Where(pair => pair.Character.JobPriorities.Any(preference =>
                preference.Value == priority && jobsInSameDept.Contains(preference.Key)));
        return Pick(job, matchingPlayerCharacters);
    }

    /// Picks a candidate from this pool for <paramref name="job"/> from absolutely all candidates in this pool. The
    /// only criteria applied is whether or not <see cref="isUserAllowedJob"/> passes for the job and user.
    public PlayerCharacter? PickCandidateIgnoringPreferences(ProtoId<JobPrototype> job) => Pick(
        job,
        _candidates.SelectMany(it => it.Value.Select(character => new PlayerCharacter(it.Key, character)))
    );

    private PlayerCharacter? Pick(ProtoId<JobPrototype> job, IEnumerable<PlayerCharacter> candidates)
    {
        var eligibleCandidates = candidates.Where(userId => isUserAllowedJob((userId.Player, userId.Character, job)))
            .ToArray();
        return eligibleCandidates.Length > 0 ? random.Pick(eligibleCandidates) : null;
    }

    /// <summary>
    /// Replaces this pool's candidates with the given <paramref name="profiles"/>.
    /// </summary>
    /// <param name="profiles">The profiles to add</param>
    /// <param name="filterAllowedJobs">
    /// A getter for what jobs a user can actually play when given the jobs they have enabled. This should evaluate
    /// absolute restrictions like playtime, whitelist, bans, etc.
    /// (This is expected to use <see cref="getEffectivePriorityForMoffMultiCharacterSelection"/>)
    /// </param>
    /// <param name="getEffectivePriorityForMoffMultiCharacterSelection">
    /// A getter for job priority in multi-character selection. <see cref="MoffCharacterSelectionManager.GetEffectivePriority"/>.
    /// </param>
    /// <remarks>
    /// This was pulled out and rewritten from WizDen's job assignment code. Function values are passed for parameters
    /// which made more sense to be decoupled or when they needed system or event-firing dependencies.
    /// Higher order functions and functional programming, yo.
    /// </remarks>
    public void SetCandidates(
        IEnumerable<(NetUserId, HashSet<HumanoidCharacterProfile>)> profiles,
        Func<NetUserId, IEnumerable<ProtoId<JobPrototype>>, IEnumerable<ProtoId<JobPrototype>>> filterAllowedJobs,
        Func<NetUserId, ProtoId<JobPrototype>, HumanoidCharacterProfile, JobPriority>
            getEffectivePriorityForMoffMultiCharacterSelection
    )
    {
        _candidates.Clear();
        _candidatesByJobAndPriority.Clear();

        // Add each player...
        foreach (var (player, userProfiles) in profiles)
        {
            // ... by adding all their profiles to the basic user-to-profiles dict...
            _candidates[player] = userProfiles;

            // ... and by adding each job from each profile to the by-job dict.
            foreach (var profile in userProfiles)
            {
                // Only add the jobs that're allowed.
                var allowedJobs = filterAllowedJobs(player, profile.JobPriorities.Keys);
                foreach (var jobId in allowedJobs)
                {
                    var priority = getEffectivePriorityForMoffMultiCharacterSelection(player, jobId, profile);
                    if (priority == JobPriority.Never)
                        continue;

                    if (!isUserAllowedJob((player, profile, jobId)))
                        continue;

                    AddToCandidates(jobId, player, profile, priority);
                }
            }
        }

        // This function just adds the job/player/character/priority combination to `_candidatesByJobAndPriority` while dealing
        // with missing intermediate dictionaries.
        void AddToCandidates(
            ProtoId<JobPrototype> job,
            NetUserId player,
            HumanoidCharacterProfile profile,
            JobPriority priority
        )
        {
            if (!_candidatesByJobAndPriority.TryGetValue(job, out var priorities))
            {
                priorities = new Dictionary<JobPriority, HashSet<PlayerCharacter>>();
                _candidatesByJobAndPriority.Add(job, priorities);
            }

            if (!priorities.TryGetValue(priority, out var players))
            {
                players = [];
                priorities.Add(priority, players);
            }

            players.Add(new PlayerCharacter(player, profile));
        }
    }
}

/// <summary>
/// A <see cref="RoundstartStationJob.Comparer">sortable</see> <see cref="JobPrototype"/> and related info for use in
/// <see cref="StationJobsSystem.AssignJobs"/>. Objects are sorted first by <see cref="FillPriority"/>, then by
/// <see cref="RoundstartStationJob.Comparer.WeightGetter">weight</see>, then by <see cref="FallbackLevel"/>, then by
/// <see cref="Priority"/>, then by <see cref="Repetition"/>, then by <see cref="Salt"/>.
/// </summary>
/// <param name="FillPriority">
/// A number that exists simply to make some objects sort before others. This is used to prioritize a number of slots to
/// be filled before others, even when jobs have the same weight. Like job weights, higher priorities are filled earlier.
/// </param>
/// <param name="Priority">
/// The <see cref="JobPriority"/> a candidate must have set for <see cref="Job"/> (or other jobs -- see
/// <see cref="MinimumJobFallback"/>) in order to be selected.
/// </param>
/// <param name="FallbackLevel">
/// How loosely we're currently willing to match a candidate to <see cref="Job"/>.
/// See <see cref="StationJobsSystem.DowngradeStrictness"/>.
/// </param>
/// <param name="Salt">
/// A number that exists simply to make some objects sort before others. This is used to make otherwise equal-sorting
/// objects sort differently. For example, if Botanist and Clown have the same weight and fill priority, due to innate
/// enumeration order (ie. lexical order), the Botanist role may always fill before the Clown role. By initializing
/// `Salt` with random numbers in different rounds, we can introduce variety to fill order.
/// </param>
/// <param name="Repetition">
/// This value is used to preserve insertion order when reinserting a value. This effectively causes jobs with the same
/// priority to be filled in a round-robin fashion.
/// </param>
/// <param name="Slots">
/// The number of slots. Null means an unlimited number, but it also stops <see cref="FallbackLevel"/> from ever
/// broadening past <see cref="MinimumJobFallback.None"/>; see <see cref="StationJobsSystem.DowngradeStrictness"/>.
/// </param>
public readonly record struct RoundstartStationJob(
    ProtoId<JobPrototype> Job,
    EntityUid Station,
    JobPriority Priority,
    MinimumJobFallback FallbackLevel,
    int? Slots,
    int FillPriority,
    int Salt,
    int Repetition
)
{
    public RoundstartStationJob(ProtoId<JobPrototype> Job, EntityUid Station, int? Slots, int FillPriority, int Salt) :
        this(Job, Station, JobPriority.High, MinimumJobFallback.None, Slots, FillPriority, Salt, 0)
    {
    }

    public override string ToString()
    {
        return $"({Job.Id}, fill={FillPriority}, {Priority})";
    }

    /// A comparer for <see cref="RoundstartStationJob"/>s which uses <see cref="WeightGetter"/>. Sorts by
    /// <see cref="RoundstartStationJob.FillPriority"/>, then <see cref="WeightGetter"/>, then
    /// <see cref="RoundstartStationJob.FallbackLevel"/>, then <see cref="RoundstartStationJob.Priority"/>, then
    /// <see cref="RoundstartStationJob.Repetition"/>, then <see cref="RoundstartStationJob.Salt"/>.
    public readonly record struct Comparer(
        Func<RoundstartStationJob, int> WeightGetter
    ) : IComparer<RoundstartStationJob>
    {
        public int Compare(RoundstartStationJob x, RoundstartStationJob y)
        {
            if (x.FillPriority.CompareTo(y.FillPriority) is var fillPriorityComparison and not 0)
                return fillPriorityComparison;

            if (WeightGetter(x).CompareTo(WeightGetter(y)) is var weightComparison and not 0)
                return weightComparison;

            if (x.FallbackLevel.CompareTo(y.FallbackLevel) is var fallbackPriorityComparison and not 0)
                return fallbackPriorityComparison;

            if (x.Priority.CompareTo(y.Priority) is var priorityComparison and not 0)
                return priorityComparison;

            if (x.Repetition.CompareTo(y.Repetition) is var repetitionComparison and not 0)
                return repetitionComparison;

            return x.Salt.CompareTo(y.Salt);
        }
    }
}

/// Ye olde newtype of <c>(<see cref="NetUserId"/>, <see cref="HumanoidCharacterProfile"/>)</c>.
/// Just slightly smaller to type and easier to understand compared to the tuple.
public readonly record struct PlayerCharacter(NetUserId Player, HumanoidCharacterProfile Character);
