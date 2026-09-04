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
/// A predicate which is used to determine if the given user is allowed to be assigned the given job. This check is
/// always honored, regardless of picking method. It should check things like job bans or playtime restrictions.
/// </param>
/// <param name="sameDepartmentJobs">
/// This function is used to retrieve alternate jobs allowed when using <see cref="PickSameDepartmentCandidate"/>.
/// Although the name specifically mentions "department", this function could be used to return any alternate jobs.
/// </param>
public sealed partial class RoundstartJobCandidates(
    IRobustRandom random,
    Predicate<(NetUserId, ProtoId<JobPrototype>)> isUserAllowedJob,
    Func<ProtoId<JobPrototype>, IEnumerable<ProtoId<JobPrototype>>> sameDepartmentJobs
)
{
    /// This constructor just instantiates and initializes this object. It's literally equivalent to calling the default
    /// constructor and then calling <see cref="SetCandidates"/>. See those for documentation of the parameters.
    /// <seealso cref="RoundstartJobCandidates"/>
    /// <seealso cref="SetCandidates"/>
    public RoundstartJobCandidates(
        IRobustRandom random,
        Predicate<(NetUserId, ProtoId<JobPrototype>)> isUserAllowedJob,
        Func<ProtoId<JobPrototype>, IEnumerable<ProtoId<JobPrototype>>> sameDepartmentJobs,
        IEnumerable<(NetUserId, HumanoidCharacterProfile)> profiles,
        Func<NetUserId, IEnumerable<ProtoId<JobPrototype>>, IEnumerable<ProtoId<JobPrototype>>> filterAllowedJobs,
        Func<NetUserId, ProtoId<JobPrototype>, HumanoidCharacterProfile, JobPriority>
            getEffectivePriorityForMoffMultiCharacterSelection
    ) : this(random, isUserAllowedJob, sameDepartmentJobs)
    {
        SetCandidates(profiles, filterAllowedJobs, getEffectivePriorityForMoffMultiCharacterSelection);
    }

    /// The basic user-to-profile collection of candidates. Used to pick candidates without caring about priorities, etc.
    private readonly Dictionary<NetUserId, HumanoidCharacterProfile> _candidates = new();

    /// A collection of users keyed by job and priority. Used to select player jobs by their requested priorities.
    private readonly Dictionary<ProtoId<JobPrototype>, Dictionary<JobPriority, HashSet<NetUserId>>>
        _candidatesByJobAndPriority = new();

    public bool IsEmpty() => _candidates.Count == 0 &&
                             _candidatesByJobAndPriority.Values.Sum(usersByPriority =>
                                 usersByPriority.Values.Sum(users => users.Count)) == 0;

    /// Removes a candidate from this pool, meaning it cannot be selected by <see cref="GetCandidate"/> or similar
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
                r2 |= users.Remove(candidate);
            }
        }

        return r1 || r2;
    }

    /// Picks a candidate for <paramref name="job"/> from this pool. <paramref name="jobFallback"/> determines exactly
    /// how this works, but it will always delegate to <see cref="PickCandidate"/>, <see cref="PickSameDepartmentCandidate"/>,
    /// and/or <see cref="PickCandidateIgnoringPreferences"/>.
    public NetUserId? GetCandidate(
        ProtoId<JobPrototype> job,
        JobPriority priority,
        MinimumJobFallback jobFallback
    ) => jobFallback switch
    {
        MinimumJobFallback.None => PickCandidate(job, priority),
        MinimumJobFallback.SameDepartment => PickCandidate(job, priority) ??
                                             PickSameDepartmentCandidate(job, priority),
        MinimumJobFallback.AnyEligiblePlayer => PickCandidate(job, priority) ??
                                                PickSameDepartmentCandidate(job, priority) ??
                                                PickCandidateIgnoringPreferences(job),
        _ => throw new ArgumentOutOfRangeException(),
    };

    /// Picks a candidate from this pool for <paramref name="job"/> at <paramref name="priority"/>.
    public NetUserId? PickCandidate(ProtoId<JobPrototype> job, JobPriority priority)
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
    public NetUserId? PickSameDepartmentCandidate(ProtoId<JobPrototype> job, JobPriority priority)
    {
        var jobsInSameDept = sameDepartmentJobs(job);
        var matchingProfiles = _candidates
            .Where(pair =>
                pair.Value.JobPriorities.Any(preference =>
                    preference.Value == priority && jobsInSameDept.Contains(preference.Key)
                )
            )
            .Select(it => it.Key);
        return Pick(job, matchingProfiles);
    }

    /// Picks a candidate from this pool for <paramref name="job"/> from absolutely all candidates in this pool. The
    /// only criteria applied is whether or not <see cref="isUserAllowedJob"/> passes for the job and user.
    public NetUserId? PickCandidateIgnoringPreferences(ProtoId<JobPrototype> job) => Pick(job, _candidates.Keys);

    private NetUserId? Pick(ProtoId<JobPrototype> job, IEnumerable<NetUserId> candidates)
    {
        var eligibleCandidates = candidates.Where(userId => isUserAllowedJob((userId, job))).ToHashSet();
        return eligibleCandidates.Count > 0 ? random.Pick(eligibleCandidates) : null;
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
        IEnumerable<(NetUserId, HumanoidCharacterProfile)> profiles,
        Func<NetUserId, IEnumerable<ProtoId<JobPrototype>>, IEnumerable<ProtoId<JobPrototype>>>
            filterAllowedJobs,
        Func<NetUserId, ProtoId<JobPrototype>, HumanoidCharacterProfile, JobPriority>
            getEffectivePriorityForMoffMultiCharacterSelection
    )
    {
        _candidates.Clear();
        _candidatesByJobAndPriority.Clear();

        // Add each profile...
        foreach (var (player, profile) in profiles)
        {
            // ... by adding it to the basic user-to-profile dict...
            _candidates[player] = profile;

            // ... and by calculating the viability of actual job selections they've made.
            foreach (var jobId in filterAllowedJobs(player, profile.JobPriorities.Keys))
            {
                // Moff Start - Job priority is a property of the player, not of the character.
                // Also note that profileJobs may now contain jobs which came from the player's
                // *other* active characters (see MoffJobCandidateSystem), so indexing this
                // profile's own priorities would throw.
                var priority = getEffectivePriorityForMoffMultiCharacterSelection(player, jobId, profile);

                if (priority == JobPriority.Never)
                    continue;

                // if (!profile.JobPriorities.TryGetValue(jobId, out var priority) || priority == JobPriority.Never)
                //     continue;
                // Moff end

                if (!isUserAllowedJob((player, jobId)))
                    continue;

                AddToCandidates(jobId, player, priority);
            }
        }

        // This function just adds the job/player/priority combination to `_candidatesByJobAndPriority` while dealing
        // with missing intermediate dictionaries.
        void AddToCandidates(ProtoId<JobPrototype> job, NetUserId player, JobPriority priority)
        {
            if (!_candidatesByJobAndPriority.TryGetValue(job, out var priorities))
            {
                priorities = new Dictionary<JobPriority, HashSet<NetUserId>>();
                _candidatesByJobAndPriority.Add(job, priorities);
            }

            if (!priorities.TryGetValue(priority, out var players))
            {
                players = [];
                priorities.Add(priority, players);
            }

            players.Add(player);
        }
    }
}

/// <summary>
/// A <see cref="RoundstartStationJob.Comparer">sortable</see> <see cref="JobPrototype"/> and related info for use in
/// <see cref="StationJobsSystem.AssignJobs"/>. Objects are sorted first by <see cref="FillPriority"/>, then by
/// <see cref="RoundstartStationJob.Comparer.WeightGetter">weight</see>, then by <see cref="Salt"/>.
/// </summary>
/// <param name="FillPriority">
/// A number that exists simply to make some objects sort before others. This is used to prioritize a number of slots to
/// be filled before others, even when jobs have the same weight. Like job weights, higher priorities are filled earlier.
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
/// <param name="Slots">The number of slots. Null means an unlimited number.</param>
public readonly record struct RoundstartStationJob(
    ProtoId<JobPrototype> Job,
    EntityUid Station,
    JobPriority Priority,
    int? Slots,
    int FillPriority,
    int Salt,
    int Repetition = 0
)
{
    public override string ToString()
    {
        return $"({Job.Id}, fill={FillPriority}, {Priority})";
    }

    /// A comparer for <see cref="RoundstartStationJob"/>s which uses <see cref="WeightGetter"/>.
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

            if (x.Priority.CompareTo(y.Priority) is var priorityComparison and not 0)
                return priorityComparison;

            if (x.Repetition.CompareTo(y.Repetition) is var repetitionComparison and not 0)
                return repetitionComparison;

            return x.Salt.CompareTo(y.Salt);
        }
    }
}
