using System.Linq;
using Content.Server._Moffstation.Preferences;
using Content.Server.Players.JobWhitelist;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Events;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Station;

/// <summary>
/// Sources round-start job candidacy from every one of a player's active characters rather than just
/// the selected one. Uses <see cref="StationJobsGetCandidatesEvent"/> in the opposite direction to
/// JobWhitelistSystem and PlayTimeTrackingSystem, which narrow the same list.
/// </summary>
public sealed partial class MoffJobCandidateSystem : EntitySystem
{
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private MoffCharacterSelectionManager _selection = default!;

    /// <summary>Every character of <paramref name="player"/> whose slot is active.</summary>
    public List<HumanoidCharacterProfile> GetActiveProfiles(NetUserId player)
    {
        var result = new List<HumanoidCharacterProfile>();

        if (!_prefs.TryGetCachedPreferences(player, out var prefs))
            return result;

        var state = _selection.GetState(player);

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile != null && state.IsSlotEnabled(slot))
                result.Add(profile);
        }

        return result;
    }
}
