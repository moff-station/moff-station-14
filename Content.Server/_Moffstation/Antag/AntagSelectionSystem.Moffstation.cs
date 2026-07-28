using System.Linq;
using Content.Server._Moffstation.Preferences;
using Content.Server._Moffstation.Station;
using Content.Shared.GameTicking.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

// Partial of the upstream AntagSelectionSystem, so it must sit in the upstream namespace.
namespace Content.Server.Antag;

/// <summary>
/// Multi-character selection support: a player opts in to an antag if any of their active
/// characters wants it, and the character that spawns must be one of those.
/// </summary>
public sealed partial class AntagSelectionSystem
{
    [Dependency] private readonly MoffCharacterSelectionManager _moffCharacterSelection = default!;

    // Resolved on demand; a mutual [Dependency] with MoffCharacterPickerSystem would be circular.
    private MoffCharacterPickerSystem MoffCharacterPicker => EntityManager.System<MoffCharacterPickerSystem>();

    /// <summary>
    /// Every antag preference held by any of the player's active characters.
    /// </summary>
    public HashSet<ProtoId<AntagPrototype>> GetMoffEnabledAntagPreferences(ICommonSession session)
    {
        var result = new HashSet<ProtoId<AntagPrototype>>();

        if (!_pref.TryGetCachedPreferences(session.UserId, out var prefs))
            return result;

        var state = _moffCharacterSelection.GetState(session.UserId);

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is not HumanoidCharacterProfile humanoid)
                continue;

            if (!state.IsSlotEnabled(slot))
                continue;

            result.UnionWith(humanoid.AntagPreferences);
        }

        return result;
    }

    /// <summary>
    /// Per preselected antag, the prototypes a character must have enabled to fill that slot.
    /// </summary>
    public List<HashSet<ProtoId<AntagPrototype>>> GetMoffPreSelectedAntagPrefRoles(ICommonSession session)
    {
        var result = new List<HashSet<ProtoId<AntagPrototype>>>();

        var query = QueryAllRules();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid))
                continue;

            foreach (var antag in comp.Antags)
            {
                if (!comp.PreSelectedSessions.TryGetValue(antag, out var set) || !set.Contains(session))
                    continue;

                if (!ProtoMan.Resolve(antag.Proto, out var proto))
                    continue;

                if (proto.PrefRoles.Count == 0)
                    continue;

                result.Add(proto.PrefRoles.ToHashSet());
            }
        }

        return result;
    }
}
