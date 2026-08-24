using System.Linq;
using Content.Client._Moffstation.Lobby.UI;
using Content.Client._Moffstation.Preferences;
using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class CharacterSetupGui
{
    private MoffJobPriorityEditor? _moffJobPriorityEditor;

    /// <summary>
    /// Job priorities apply to the whole player, so they get their own pane next to the character
    /// editor rather than living inside any one character.
    /// </summary>
    private void InitializeMoffJobPriorities()
    {
        _moffJobPriorityEditor = new MoffJobPriorityEditor();
        JobPriorityEditor.AddChild(_moffJobPriorityEditor);

        JobPrioritiesButton.OnPressed += _ =>
        {
            _moffJobPriorityEditor.LoadJobPriorities();
            ShowMoffJobPriorities(true);
        };
    }

    private void ShowMoffJobPriorities(bool show)
    {
        CharEditor.Visible = !show;
        JobPriorityEditor.Visible = show;
        JobPrioritiesButton.Pressed = show;
    }

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
