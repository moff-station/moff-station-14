using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Station.Systems;

/// <summary>
/// Moffstation: Triggered adjustments to roundstart jobs. 
/// </summary>
public sealed partial class StationJobsSystem : EntitySystem
{
    /// Nice lil helper for when you have a job prototype instead of a string.
    public bool TryAdjustRoundstartJobSlot(EntityUid station, JobPrototype job, int amount, bool createSlot = false, bool clamp = false,
        StationJobsComponent? stationJobs = null)
    {
        return TryAdjustRoundstartJobSlot(station, job.ID, amount, createSlot, clamp, stationJobs);
    }

    /// This is exactly TryAdjustJobSlot but for roundstart jobs.
    /// Realistically this should be refactorable, but imo the original is a bit of a mess,
    /// and the best practice is to not screw with upstream for this kind of thing.
    /// TODO: Refactor and PR the whole job system to upstream. I guess.
    public bool TryAdjustRoundstartJobSlot(EntityUid station,
        string jobPrototypeId,
        int amount,
        bool createSlot = false,
        bool clamp = false,
        StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var jobList = stationJobs.SetupAvailableJobs;

        // This should:
        // - Return true when zero slots are added/removed.
        // - Return true when you add.
        // - Return true when you remove and do not exceed the number of slot available.
        // - Return false when you remove from a job that doesn't exist.
        // - Return false when you remove and exceed the number of slots available.
        // And additionally, if adding would add a job not previously on the manifest when createSlot is false, return false and do nothing.

        if (amount == 0)
            return true;

        switch (jobList.TryGetValue(jobPrototypeId, out var available))
        {
            case false when amount < 0:
                return false;
            case false:
                if (!createSlot)
                    return false;
                stationJobs.TotalJobs += amount;
                jobList[jobPrototypeId] = [amount, amount];
                UpdateJobsAvailable();
                return true;
            case true:
                // Job is unlimited so just say we adjusted it and do nothing.
                if (available is not {} _)
                    return true;

                int avail = available.First();

                // Would remove more jobs than we have available.
                if (avail + amount < 0 && !clamp)
                    return false;

                int newAmount = Math.Max(avail + amount, 0);
                jobList[jobPrototypeId] = [newAmount, newAmount];
                stationJobs.TotalJobs += newAmount;
                UpdateJobsAvailable();
                return true;
        }
    }
}
