using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

// Partial of the upstream PlayTimeTrackingSystem, so it must sit in the upstream namespace.
namespace Content.Server.Players.PlayTimeTracking;

public sealed partial class PlayTimeTrackingSystem
{
    /// <summary>
    /// Same as <see cref="IsAllowed(ICommonSession, ProtoId{JobPrototype})"/>, but checks a
    /// character other than the selected one. Multi-character selection has to test each of a
    /// player's active characters against the job they were assigned.
    /// </summary>
    /// <remarks>
    /// Unlike the upstream overload this still checks age, species and traits when role timers are
    /// off. Without that, picking which character spawns would ignore those gates entirely.
    /// </remarks>
    public bool IsAllowed(ICommonSession player, ProtoId<JobPrototype> job, HumanoidCharacterProfile profile)
    {
        var requirements = _roles.GetRoleRequirements(job);

        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            requirements = StripPlaytimeRequirements(requirements);

        if (!_tracking.TryGetTrackerTimes(player, out var playTimes))
        {
            Log.Error($"Unable to check playtimes {Environment.StackTrace}");
            playTimes = new Dictionary<string, TimeSpan>();
        }

        return JobRequirements.TryRequirementsMet(requirements, playTimes, out _, EntityManager, ProtoMan, profile);
    }

    /// <summary>
    /// Drops the requirements that role timers govern, leaving the ones that describe the character
    /// itself. Returns null when nothing is left, which <see cref="JobRequirements"/> treats as met.
    /// </summary>
    private static HashSet<JobRequirement>? StripPlaytimeRequirements(HashSet<JobRequirement>? requirements)
    {
        if (requirements == null)
            return null;

        return requirements
            .Where(requirement => requirement is not (
                RoleTimeRequirement or
                DepartmentTimeRequirement or
                OverallPlaytimeRequirement))
            .ToHashSet();
    }
}
