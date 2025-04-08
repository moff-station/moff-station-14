using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Umbra.Preferences.Loadouts.Effects;

/// <summary>
///     Implements a loadout effect that restricts items to a specific character,
///     based on the currently selected character's name.
/// </summary>
/// <remarks>
///     Sector Umbra
/// </remarks>
public sealed partial class PersonalItemLoadoutEffect : LoadoutEffect
{
    [DataField("character", required: true)]
    public HashSet<string> CharacterName = default!;

    [DataField]
    public HashSet<ProtoId<RoleLoadoutPrototype>> Jobs = new();

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var foundMatchingName = false;
        foreach (var name in CharacterName)
        {
            if (name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase))
            {
                foundMatchingName = true;
                break;
            }
        }

        if (!foundMatchingName)
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString(
                "loadout-personalitem-character",
                ("character", string.Join(", ", CharacterName))));
            return false;
        }

        if (!(Jobs.Count == 0 || Jobs.Contains(loadout.Role)))
        {
            var jobNames = new List<string>();
            foreach (var jobId in Jobs)
            {
                jobNames.Add(Loc.GetString(jobId));
            }

            reason = FormattedMessage.FromUnformatted(Loc.GetString(
                "loadout-personalitem-joblocked",
                ("job", string.Join(", ", jobNames)),
                ("received", Loc.GetString(loadout.Role))));

            return false;
        }

        reason = null;
        return true;
    }
}
