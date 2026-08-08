using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Moffstation.Preferences;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Database
{
    public abstract partial class ServerDbBase
    {
        #region Multi-Character Selection

        /// <summary>
        /// Loads a player's multi-character selection state: their player-global job priorities and
        /// which character slots are active.
        /// </summary>
        /// <remarks>
        /// If the player has no state yet, one is created and seeded from the job priorities of
        /// their currently selected character, so that players who predate multi-character
        /// selection keep their existing setup on first login.
        /// </remarks>
        public async Task<MoffCharacterSelectionState> GetMoffCharacterSelectionAsync(
            NetUserId userId,
            CancellationToken cancel = default)
        {
            await using var db = await GetDb(cancel);

            var prefs = await db.DbContext.Preference
                .Include(p => p.MoffPreference)
                    .ThenInclude(mp => mp!.JobPriorities)
                .Include(p => p.Profiles)
                    .ThenInclude(h => h.MoffProfile)
                .Include(p => p.Profiles)
                    .ThenInclude(h => h.Jobs)
                .AsSplitQuery()
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);

            var state = new MoffCharacterSelectionState { IsAuthoritative = true };

            if (prefs == null)
                return state;

            if (prefs.MoffPreference == null)
            {
                // First login since multi-character selection was added. Seed the player-global
                // priorities from every character, keeping the highest priority each job was given,
                // so a role that only lived on a non-selected character isn't silently lost.
                var seeded = new Dictionary<string, DbJobPriority>();

                foreach (var profile in prefs.Profiles)
                {
                    foreach (var job in profile.Jobs)
                    {
                        if (!seeded.TryGetValue(job.JobName, out var existing) || job.Priority > existing)
                            seeded[job.JobName] = job.Priority;
                    }
                }

                // Only one job may be High, so keep whichever the selected character had rather than
                // leaving it to Normalize, which picks by dictionary enumeration order.
                var selected = prefs.Profiles.FirstOrDefault(p => p.Slot == prefs.SelectedCharacterSlot);
                var keepHigh = selected?.Jobs.FirstOrDefault(j => j.Priority == DbJobPriority.High)?.JobName
                               ?? seeded.Where(kv => kv.Value == DbJobPriority.High)
                                   .Select(kv => kv.Key)
                                   .OrderBy(name => name)
                                   .FirstOrDefault();

                foreach (var jobName in seeded.Keys.ToList())
                {
                    if (seeded[jobName] == DbJobPriority.High && jobName != keepHigh)
                        seeded[jobName] = DbJobPriority.Medium;
                }

                var moffPrefs = new MoffModel.MoffPreference { PreferenceId = prefs.Id };

                foreach (var (jobName, priority) in seeded)
                {
                    moffPrefs.JobPriorities.Add(new MoffModel.MoffJobPriority
                    {
                        JobName = jobName,
                        Priority = priority,
                    });
                }

                db.DbContext.Add(moffPrefs);
                await db.DbContext.SaveChangesAsync(cancel);

                prefs.MoffPreference = moffPrefs;
            }

            foreach (var priority in prefs.MoffPreference.JobPriorities)
            {
                state.JobPriorities[priority.JobName] = (JobPriority)priority.Priority;
            }

            foreach (var profile in prefs.Profiles)
            {
                state.EnabledSlots[profile.Slot] = profile.MoffProfile?.Enabled ?? true;
            }

            state.Normalize();

            return state;
        }

        /// <summary>
        /// Replaces a player's player-global job priorities wholesale.
        /// </summary>
        public async Task SaveMoffJobPrioritiesAsync(
            NetUserId userId,
            Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
        {
            await using var db = await GetDb();

            var prefs = await db.DbContext.Preference
                .Include(p => p.MoffPreference)
                    .ThenInclude(mp => mp!.JobPriorities)
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId);

            if (prefs == null)
                return;

            if (prefs.MoffPreference == null)
            {
                prefs.MoffPreference = new MoffModel.MoffPreference { PreferenceId = prefs.Id };
                db.DbContext.Add(prefs.MoffPreference);
            }
            else
            {
                db.DbContext.RemoveRange(prefs.MoffPreference.JobPriorities);
                prefs.MoffPreference.JobPriorities.Clear();
            }

            foreach (var (job, priority) in priorities)
            {
                if (priority == JobPriority.Never)
                    continue;

                prefs.MoffPreference.JobPriorities.Add(new MoffModel.MoffJobPriority
                {
                    JobName = job.Id,
                    Priority = (DbJobPriority)priority,
                });
            }

            await db.DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Marks a single character slot active or inactive.
        /// </summary>
        public async Task SaveMoffCharacterEnabledAsync(NetUserId userId, int slot, bool enabled)
        {
            await using var db = await GetDb();

            var profile = await db.DbContext.Profile
                .Include(p => p.Preference)
                .Include(p => p.MoffProfile)
                .Where(p => p.Preference.UserId == userId.UserId)
                .AsSplitQuery()
                .SingleOrDefaultAsync(p => p.Slot == slot);

            if (profile == null)
                return;

            if (profile.MoffProfile == null)
            {
                profile.MoffProfile = new MoffModel.MoffProfile
                {
                    ProfileId = profile.Id,
                    Enabled = enabled,
                };
                db.DbContext.Add(profile.MoffProfile);
            }
            else
            {
                profile.MoffProfile.Enabled = enabled;
            }

            await db.DbContext.SaveChangesAsync();
        }

        #endregion
    }
}
