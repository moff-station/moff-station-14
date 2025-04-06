using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
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
    public HashSet<string> Jobs = new();

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

        if (!(Jobs.Count == 0 || Jobs.Contains(loadout.Role.ToString())))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString(
                "loadout-personalitem-joblocked",
                ("job", string.Join(", ", Jobs)),
                ("received", loadout.Role.ToString())));
            return false;
        }

        reason = null;
        return true;
    }
}
