using System.Diagnostics.CodeAnalysis;
using Content.Shared._Umbra.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Uses a <see cref="LoadoutEffectGroupPrototype"/> prototype as a singular effect that can be re-used.
/// </summary>
public sealed partial class GroupLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public ProtoId<LoadoutEffectGroupPrototype> Proto;

    // Moffstation - Add documentation for the addition of personal items
    /// <summary>
    /// Validates the loadout effect group against the given profile.
    /// </summary>
    /// <param name="profile">The humanoid character profile.</param>
    /// <param name="loadout">The role loadout.</param>
    /// <param name="session">The common session, if any.</param>
    /// <param name="collection">The dependency collection.</param>
    /// <param name="reason">The reason for validation failure, if any.</param>
    /// <returns>True if the validation is successful, otherwise false.</returns>
    /// <remarks>Umbra change: personal loadout items are validated using an override method in <see cref="PersonalItemLoadoutEffect"/>.
    /// Personal loadout related data is passed through HumanoidCharacterProfile profile.</remarks>
    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var effectsProto = collection.Resolve<IPrototypeManager>().Index(Proto);

        var reasons = new List<string>();
        foreach (var effect in effectsProto.Effects)
        {
            if (effect.Validate(profile, loadout, session, collection, out reason))
                continue;

            reasons.Add(reason.ToMarkup());
        }

        reason = reasons.Count == 0 ? null : FormattedMessage.FromMarkupOrThrow(string.Join('\n', reasons));
        return reason == null;
    }
}
