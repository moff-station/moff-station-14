using System.Linq;
using Content.Server._Moffstation.Preferences;
using Content.Server._Moffstation.Station.Systems;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Station.Events;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

// ReSharper disable once CheckNamespace // Partial part of upstream system
namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private MoffCharacterSelectionManager _moffCharacterSelection = default!;
    [Dependency] private ISharedPlayerManager _playerMan = default!;
    [Dependency] private PlayTimeTrackingSystem _playTimeTracking = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    /// <summary>
    /// <see cref="AssignJobs"/> + <see cref="AssignOverflowJobs"/>, as the name implies :gosomnia:.
    /// </summary>
    /// <param name="readyPlayers">The players who are ready and should be considered for job-assignment.</param>
    /// <param name="stations">The stations whose jobs are to be assigned.</param>
    /// <returns>Players and their assigned job, station, and profile.</returns>
    /// <remarks> Moffstation combines them so that user-to-profile logic can be run once and reused for both, since
    /// it involves some expensive lookups.</remarks>
    public Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid Station, HumanoidCharacterProfile)>
        AssignJobsAndOverflowJobs(IEnumerable<NetUserId> readyPlayers, IReadOnlyList<EntityUid> stations) =>
        AssignJobsAndOverflowJobs(_moffCharacterSelection.GetActiveProfiles(readyPlayers), stations);

    /// <summary>
    /// <see cref="AssignJobsAndOverflowJobs(IEnumerable{NetUserId},IReadOnlyList{Robust.Shared.GameObjects.EntityUid})"/>,
    /// but with profiles already resolved and a flag to allow for skipping overflow assignments.
    /// </summary>
    /// <remarks>
    /// This realistically should only be used for tests, we normally don't want to circumvent standard profile lookup
    /// or overflow assignments.
    /// </remarks>
    public Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid Station, HumanoidCharacterProfile)>
        AssignJobsAndOverflowJobs(
            Dictionary<NetUserId, HashSet<HumanoidCharacterProfile>> readyPlayersAndActiveProfiles,
            IReadOnlyList<EntityUid> stations,
            bool doOverflowAssignments = true
        )
    {
        var candidates = CreateCandidatePool(readyPlayersAndActiveProfiles);
        var assignedJobs = AssignJobs(candidates, stations);
        if (doOverflowAssignments)
        {
            AssignOverflowJobs(ref assignedJobs, candidates.Candidates.Keys, candidates.Candidates, stations);
        }

        return assignedJobs;
    }

    /// <summary>
    /// Assigns jobs based on the given preferences and list of stations to assign for.
    /// This does NOT change the slots on the station, only figures out where each player should go.
    /// </summary>
    /// <param name="candidates">The players and their profiles to consider assigning.</param>
    /// <param name="stations">List of stations to assign for.</param>
    /// <returns>List of players and their assigned jobs.</returns>
    /// <remarks>
    /// This is a total rewrite of upstream's implementation. Compared to that, we respect player's job priorities much
    /// more.
    /// </remarks>
    public Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid Station, HumanoidCharacterProfile)> AssignJobs(
        RoundstartJobCandidates candidates,
        IReadOnlyList<EntityUid> stations
    )
    {
        DebugTools.Assert(stations.Count > 0);

        if (candidates.IsEmpty())
            return new();

        // The priority queue replaces upstream's two-phase selection. We just sort the jobs by what is most important
        // and loop over greedily assigning the top priority job.
        // The power of the priority queue is in that we don't need separate phases with different logic nor do we need
        // to do any annoying tracking of what's important; we just describe the job and the queue's sorting figures
        // out what is the priority to be filled.
        var requiredJobsPq = CreateRoundstartStationJobPriorityQueue(stations);
        var jobAssignments = new Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid, HumanoidCharacterProfile)>();
        var jobFallback = _configurationManager.GetCVar(CCVars.GameMinimumJobFallback);

        // Take the most important job from the front of the queue and try to assign it from `candidates`.
        while (requiredJobsPq.TakeOrNull() is
               (var job, var station, var priority, var fallbackLevel, var slots, _, _, _) sort)
        {
            // If there're no candidates, we can't assign any more jobs.
            if (candidates.IsEmpty())
                break;

            DebugTools.AssertNotEqual(slots, 0);

            var candidateNullable = fallbackLevel switch
            {
                MinimumJobFallback.None => candidates.PickCandidate(job, priority),
                MinimumJobFallback.SameDepartment => candidates.PickSameDepartmentCandidate(job, priority),
                MinimumJobFallback.AnyEligiblePlayer => candidates.PickCandidateIgnoringPreferences(job),
                _ => this.UnknownEnumVariant<MinimumJobFallback, PlayerCharacter?>(fallbackLevel),
            };
            if (candidateNullable is not { } candidate)
            {
                // If there are absolutely no candidates for this job, relax how strict we are about candidates' preferences.
                if (DowngradeStrictness(sort, jobFallback) is { } lessStrict)
                {
                    // Throw the relaxed-criteria job back into the queue. The queue will yield it to be filled
                    // again eventually, after we've given other higher priority jobs a chance to be filled.
                    requiredJobsPq.Add(lessStrict);
                }

                // If we couldn't relax the criteria, don't requeue the job -- nobody wants it, even with the most relaxed restrictions.
                continue;
            }

            // Assign the candidate and remove them from the pool.
            jobAssignments.Add(candidate.Player, (job, station, candidate.Character));
            var removed = candidates.Remove(candidate.Player);
            DebugTools.Assert(removed);

            // If there're still slots remaining, put it back in the queue.
            var remainingSlots = slots - 1;
            if (remainingSlots != 0)
            {
                // Decrement `Repetition` so that it is prioritized after all other items in the queue with otherwise
                // equal priority. This enforced round-robin filling of slots.
                requiredJobsPq.Add(sort with { Slots = remainingSlots, Repetition = sort.Repetition - 1 });
            }
        }

        return jobAssignments;
    }

    /// Relaxes the restrictions on which candidates can take the job described by <paramref name="current"/>, returning
    /// a new <see cref="RoundstartStationJob"/>. In the case that we cannot make the criteria any less strict, returns
    /// <c>null</c>.
    /// "relaxing" in this sense means first lowering the <see cref="JobPriority"/> at which we will take candidates and
    /// then relaxing exactly which job a candidate has to have selected to take the job, according to
    /// <see cref="MinimumJobFallback"/>. The fallback level will never go lower than
    /// <paramref name="minimumFallbackLevel"/>.
    /// When broadening the fallback level, <see cref="RoundstartStationJob.Priority"/> is reset to high. This means
    /// we'll try to give the job to somebody who has specifically asked to fill a role
    /// (ie. <see cref="MinimumJobFallback.None"/>) at low priority before we use backup filling methods (eg.
    /// <see cref="MinimumJobFallback.SameDepartment"/>) at high priority.
    /// Jobs with unlimited (<c>null</c>) <see cref="RoundstartStationJob.Slots"/> never broaden their fallback level,
    /// otherwise they would take all candidates available.
    private RoundstartStationJob? DowngradeStrictness(
        RoundstartStationJob current,
        MinimumJobFallback minimumFallbackLevel
    )
    {
        // If we're not already at the minimum priority, reduce the priority we're willing to take candidates at.
        if (current.Priority != JobPriority.Low)
        {
            return current.Priority switch
            {
                JobPriority.Never => null,
                JobPriority.Low => JobPriority.Never,
                JobPriority.Medium => JobPriority.Low,
                JobPriority.High => JobPriority.Medium,
                var e => this.UnknownEnumVariant<JobPriority, JobPriority?>(e),
            } is { } priority
                ? current with { Priority = priority }
                : null;
        }

        // Unlimited slot jobs cannot use fallbacks, otherwise they would slurp up too many candidates.
        if (current.Slots == null)
            return null;

        // If we're not already at the minimum fallback level, broaden the pool of candidates we're willing to take from
        // and reset the priority to high.
        if (current.FallbackLevel != minimumFallbackLevel)
        {
            return current.FallbackLevel switch
            {
                MinimumJobFallback.SameDepartment => MinimumJobFallback.AnyEligiblePlayer,
                MinimumJobFallback.AnyEligiblePlayer => null,
                MinimumJobFallback.None => MinimumJobFallback.SameDepartment,
                var e => this.UnknownEnumVariant<MinimumJobFallback, MinimumJobFallback?>(e),
            } is { } nextFallback
                ? current with { FallbackLevel = nextFallback, Priority = JobPriority.High }
                : null;
        }

        // We're already at our minimum criteria and nobody took the job. Stop trying to fill this job.
        return null;
    }

    /// Creates and returns a <see cref="RoundstartJobCandidates"/> from <paramref name="profiles"/>.
    private RoundstartJobCandidates CreateCandidatePool(
        Dictionary<NetUserId, HashSet<HumanoidCharacterProfile>> profiles
    )
    {
        // Antags status limits which jobs can be assigned, so we'll need this info in a couple of different places.
        // It's expensive to calculate, so we calculate it once and reuse it.
        var preselectedAntags = _antag.GetAntagJobs();

        // For users who have been preselected to be antags, filter out any profiles of theirs which do not match the
        // antag they've been selected to be. This prevents being assigned to use a profile which does not have the
        // preselected antag enabled.
        List<(NetUserId, HashSet<HumanoidCharacterProfile>)> filteredProfiles = new();
        foreach (var (user, userProfiles) in profiles)
        {
            if (!_player.TryGetSessionById(user, out var session) ||
                !preselectedAntags.TryGetValue(session, out var a) ||
                a.Roles is not { } selectedForOneOf)
            {
                // Not preselected to be antag, add all their profiles.
                filteredProfiles.Add((user, userProfiles));
                continue;
            }

            var filteredUserProfiles = userProfiles
                .Where(profile => profile.AntagPreferences.Intersect(selectedForOneOf).Any())
                .ToHashSet();
            if (filteredUserProfiles.Count > 0)
                filteredProfiles.Add((user, filteredUserProfiles));
        }

        return new RoundstartJobCandidates(
            _random,
            isUserAllowedJob: playerCharacterAndJob => IsCharacterAllowedJob(playerCharacterAndJob) &&
                                                       IsCandidateForJob(playerCharacterAndJob) &&
                                                       IsJobAllowedAsAntag(playerCharacterAndJob) &&
                                                       !IsJobBanned(playerCharacterAndJob),
            sameDepartmentJobs: job =>
            {
                _jobs.TryGetPrimaryDepartment(job.Id, out var department);
                return department?.Roles ?? [];
            },
            filteredProfiles,
            filterAllowedJobs: (user, jobs) =>
            {
                var ev = new StationJobsGetCandidatesEvent(user, [.. jobs]);
                RaiseLocalEvent(ref ev);
                return ev.Jobs;
            },
            getEffectivePriorityForMoffMultiCharacterSelection: (user, job, profile) =>
                _moffCharacterSelection.GetEffectivePriority(user, job, profile)
        );

        // Below are predicates used to build `isUserAllowedJob` in the candidate pool.

        bool IsCharacterAllowedJob(
            (NetUserId User, HumanoidCharacterProfile Character, ProtoId<JobPrototype> Job) playerCharacterAndJob)
        {
            if (!_playerMan.HasPlayerData(playerCharacterAndJob.User))
            {
                this.AssertOrLogError($"Failed to find session for player with id={playerCharacterAndJob.User}");
                return false;
            }

            return _playTimeTracking.IsAllowed(_playerMan.GetSessionById(playerCharacterAndJob.User),
                playerCharacterAndJob.Job,
                playerCharacterAndJob.Character);
        }

        bool IsCandidateForJob(
            (NetUserId User, HumanoidCharacterProfile Character, ProtoId<JobPrototype> Job) playerCharacterAndJob)
        {
            var ev = new StationJobsGetCandidatesEvent(playerCharacterAndJob.User, [playerCharacterAndJob.Job]);
            RaiseLocalEvent(ref ev);
            return ev.Jobs.Count != 0;
        }

        bool IsJobBanned(
            (NetUserId User, HumanoidCharacterProfile Character, ProtoId<JobPrototype> Job) playerCharacterAndJob)
        {
            var roleBans = _banManager.GetJobBans(playerCharacterAndJob.User);
            return roleBans != null && roleBans.Contains(playerCharacterAndJob.Job);
        }

        bool IsJobAllowedAsAntag((
            NetUserId User,
            HumanoidCharacterProfile Character,
            ProtoId<JobPrototype> Job
            ) playerCharacterAndJob
        )
        {
            if (!_player.TryGetSessionById(playerCharacterAndJob.User, out var session))
            {
                return false;
            }

            var (_, whitelist, blacklist) = preselectedAntags.GetValueOrDefault(session);
            return (whitelist == null || whitelist.Contains(playerCharacterAndJob.Job)) &&
                   (blacklist == null || !blacklist.Contains(playerCharacterAndJob.Job));
        }
    }

    /// Creates and returns a <see cref="PriorityQueue{T}"/> of <see cref="RoundstartStationJob"/>s based on the jobs
    /// defined for the given <see cref="stations"/>. The queue prioritizes jobs based on
    /// <see cref="RoundstartStationJob.Comparer"/>'s comparisons.
    private PriorityQueue<RoundstartStationJob> CreateRoundstartStationJobPriorityQueue(
        IReadOnlyList<EntityUid> stations
    )
    {
        var queue = new PriorityQueue<RoundstartStationJob>(new RoundstartStationJob.Comparer(job =>
            GetJobWeight(job.Station, ProtoMan.Index(job.Job)))
        );
        foreach (var station in stations)
        {
            var seenJobs = new HashSet<ProtoId<JobPrototype>>();
            var roundstartJobs = GetRoundStartJobs(station);
            var jobs = GetJobs(station);

            foreach (var (job, roundstartSlots) in roundstartJobs)
            {
                // Make sure the job exists.
                ProtoMan.Resolve(job, out _);

                // Add roundstart job slots as highest priority.
                if (roundstartSlots != 0)
                {
                    queue.Add(
                        new RoundstartStationJob(
                            job,
                            station,
                            roundstartSlots,
                            FillPriority: 0,
                            Salt: _random.Next()
                        )
                    );
                }

                // Add remaining slots as lower priority.
                if (jobs.TryGetValue(job, out var allSlots))
                {
                    int? allSlotsMinusRoundstart;
                    if (roundstartSlots == null)
                        allSlotsMinusRoundstart = allSlots;
                    else if (allSlots == null)
                        allSlotsMinusRoundstart = null;
                    else
                        allSlotsMinusRoundstart = allSlots - roundstartSlots;

                    if (allSlotsMinusRoundstart != 0)
                    {
                        queue.Add(
                            new RoundstartStationJob(
                                job,
                                station,
                                allSlotsMinusRoundstart,
                                FillPriority: -1,
                                Salt: _random.Next()
                            )
                        );
                    }
                }

                // Remember that we've handled this job already.
                seenJobs.Add(job);
            }

            // Anything in `jobs` not in `roundstartJobs` gets added here.
            foreach (var (job, allSlotsNullable) in jobs)
            {
                if (allSlotsNullable is { } allSlots and not 0 && !seenJobs.Contains(job))
                {
                    queue.Add(
                        new RoundstartStationJob(
                            job,
                            station,
                            allSlots,
                            FillPriority: -1,
                            Salt: _random.Next()
                        )
                    );
                }
            }
        }

        return queue;
    }
}
