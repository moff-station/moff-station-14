using System.Linq;
using Content.Client._Moffstation.Preferences;
using Content.Shared.Preferences;

// Partial of the upstream CharacterSetupGui, so it must sit in the upstream namespace.
namespace Content.Client.Lobby.UI;

public sealed partial class CharacterSetupGui
{
    /// <summary>
    /// Slots are not reindexed on deletion, so a new character can inherit a deleted one's
    /// disabled flag.
    /// </summary>
    private void CreateMoffCharacter(HumanoidCharacterProfile profile)
    {
        // Mirrors ClientPreferencesManager.CreateCharacter, which fills the lowest free slot.
        var taken = _preferencesManager.Preferences?.Characters.Keys.ToHashSet() ?? new HashSet<int>();

        int? newSlot = null;
        for (var slot = 0; slot < _preferencesManager.Settings?.MaxCharacterSlots; slot++)
        {
            if (taken.Contains(slot))
                continue;

            newSlot = slot;
            break;
        }

        _preferencesManager.CreateCharacter(profile);

        if (newSlot != null)
            IoCManager.Resolve<MoffCharacterSelectionManager>().ResetSlot(newSlot.Value);
    }
}
