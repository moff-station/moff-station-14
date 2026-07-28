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
    public bool IsAllowed(ICommonSession player, ProtoId<JobPrototype> job, HumanoidCharacterProfile profile)
    {
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        if (!_tracking.TryGetTrackerTimes(player, out var playTimes))
        {
            Log.Error($"Unable to check playtimes {Environment.StackTrace}");
            playTimes = new Dictionary<string, TimeSpan>();
        }

        var requirements = _roles.GetRoleRequirements(job);
        return JobRequirements.TryRequirementsMet(requirements, playTimes, out _, EntityManager, ProtoMan, profile);
    }
}
