using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

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
    public string CharacterName = default!;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (profile is HumanoidCharacterProfile humanoid &&
            humanoid.Name.Equals(CharacterName, StringComparison.InvariantCultureIgnoreCase))
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromUnformatted(Loc.GetString(
            "loadout-personal-item-belongs-to",
            ("character", CharacterName)));
        return false;
    }
}
