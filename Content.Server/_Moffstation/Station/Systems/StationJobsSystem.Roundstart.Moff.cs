using System.Linq;
using Content.Server._Moffstation.Preferences;
using Content.Server._Moffstation.Station.Systems;
using Content.Server.Station.Events;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

// ReSharper disable once CheckNamespace // Partial part of upstream system
namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private MoffCharacterSelectionManager _moffCharacterSelection = default!;

    /// <summary>
    /// Assigns jobs based on the given preferences and list of stations to assign for.
    /// This does NOT change the slots on the station, only figures out where each player should go.
    /// </summary>
    /// <param name="profiles">The profiles to use for selection.</param>
    /// <param name="stations">List of stations to assign for.</param>
    /// <returns>List of players and their assigned jobs.</returns>
    /// <remarks>
    /// This is a total rewrite of upstream's implementation. Compared to that, we respect player's job priorities much
    /// more.
    /// </remarks>
    public Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid Station)> AssignJobs(
        Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
        IReadOnlyList<EntityUid> stations
    )
    {
        DebugTools.Assert(stations.Count > 0);

        if (profiles.Count == 0)
            return new();

        // The candidate pool owns the exact picking logic and managing candidates who've already been picked.
        var candidates = CreateCandidatePool(profiles);
        // The priority queue replaces upstream's two-phase selection. We just sort the jobs by what is most important
        // and loop over greedily assigning the top priority job.
        // The power of the priority queue is in that we don't need separate phases with different logic nor do we need
        // to do any annoying tracking of what's important; we just describe the job and the queue's sorting figures
        // out what is the priority to be filled.
        var requiredJobsPq = CreateRoundstartStationJobPriorityQueue(stations);
        var jobAssignments = new Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)>(profiles.Count);
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
                _ => this.UnknownEnumVariant<MinimumJobFallback, NetUserId?>(fallbackLevel),
            };
            if (candidateNullable is not { } candidate)
            {
                // If there are absolutely no candidates, relax how strict we are about candidate's preferences.
                if (DowngradeStrictness(sort, jobFallback) is { } lessStrict)
                {
                    // Throw the relaxed-criteria job back into the queue. The queue will yield it to be filled
                    // again eventually, after we've given other higher priority jobs a chance to be filled.
                    requiredJobsPq.Add(lessStrict);
                }

                // If we couldn't relax the criteria, don't requeue the job -- nobody wants it.
                continue;
            }

            // Assign the candidate and remove them from the pool.
            jobAssignments.Add(candidate, (job, station));
            var removed = candidates.Remove(candidate);
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
    private RoundstartJobCandidates CreateCandidatePool(Dictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        // Pre-selected antags. Antags status limits which jobs can be assigned, so we'll need this info.
        // It's expensive to calculate, so we calculate it once and reuse it.
        var antags = _antag.GetAntagJobs();

        return new RoundstartJobCandidates(
            _random,
            isUserAllowedJob: playerAndJob => IsCandidateForJob(playerAndJob) &&
                                              IsJobAllowedAsAntag(playerAndJob) &&
                                              !IsJobBanned(playerAndJob),
            sameDepartmentJobs: job =>
            {
                _jobs.TryGetPrimaryDepartment(job.Id, out var department);
                return department?.Roles ?? [];
            },
            profiles.Select(it => (it.Key, it.Value)),
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

        bool IsCandidateForJob((NetUserId User, ProtoId<JobPrototype> Job) userAndJob)
        {
            var ev = new StationJobsGetCandidatesEvent(userAndJob.User, [userAndJob.Job]);
            RaiseLocalEvent(ref ev);
            return ev.Jobs.Count != 0;
        }

        bool IsJobBanned((NetUserId User, ProtoId<JobPrototype> Job) userAndJob)
        {
            var roleBans = _banManager.GetJobBans(userAndJob.User);
            return roleBans != null && roleBans.Contains(userAndJob.Job);
        }

        bool IsJobAllowedAsAntag((NetUserId User, ProtoId<JobPrototype> Job) userAndJob)
        {
            if (!_player.TryGetSessionById(userAndJob.User, out var session))
            {
                return false;
            }

            var (whitelist, blacklist) = antags.GetValueOrDefault(session);
            return (whitelist == null || whitelist.Contains(userAndJob.Job)) &&
                   (blacklist == null || !blacklist.Contains(userAndJob.Job));
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
