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
        while (requiredJobsPq.TakeOrNull() is (var job, var station, var priority, var slots, _, _, _) sort)
        {
            // If there're no candidates, we can't assign any more jobs.
            if (candidates.IsEmpty())
                break;

            DebugTools.AssertNotEqual(slots, 0);

            if (candidates.GetCandidate(job, priority, jobFallback) is not { } candidate)
            {
                // If there are absolutely no candidates, downgrade the priority we're willing to take.
                if (priority.NextLower() is { } nextLowerPriority and > JobPriority.Never)
                {
                    // Throw this job back into the queue with a lower priority. The queue will yield it to be filled
                    // again eventually, after we've given other higher priority jobs a chance to be filled.
                    requiredJobsPq.Add(sort with { Priority = nextLowerPriority });
                }

                // If there's no lower priority, or the next lower is `Never`, don't requeue the job -- nobody wants it.
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
                            JobPriority.High,
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
                                JobPriority.High,
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
                            JobPriority.High,
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

static file class JobPriorityExt
{
    extension(JobPriority priority)
    {
        /// Returns the <see cref="JobPriority"/> that's just lower than the receiver. Returns <c>null</c> if no
        /// such priority exists.
        public JobPriority? NextLower() => priority switch
        {
            JobPriority.Never => null,
            JobPriority.Low => JobPriority.Never,
            JobPriority.Medium => JobPriority.Low,
            JobPriority.High => JobPriority.Medium,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };
    }
}
