using Content.Client._Moffstation.LateJoin;
using Content.Client._Moffstation.Preferences;
using Content.Client.Lobby.UI;
using Content.Shared.Preferences;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;

namespace Content.Client.LateJoin;

public sealed partial class LateJoinGui
{
    [Dependency] private ISharedPlayerManager _moffPlayerManager = default!;
    [Dependency] private MoffCharacterSelectionManager _moffSelection = default!;

    private MoffLateJoinLayout _moffLayout = default!;

    /// <summary>Which character slot the player is joining as, if they have one.</summary>
    private int? MoffSelectedSlot { get; set; }

    /// <summary>
    /// Wraps the upstream job list in a two column layout with a character picker on the left.
    /// </summary>
    private Control BuildMoffLayout(Control jobList)
    {
        _moffLayout = new MoffLateJoinLayout();
        _moffLayout.JobList.AddChild(jobList);

        return _moffLayout;
    }

    private void RebuildMoffCharacterList()
    {
        _moffLayout.CharacterList.RemoveAllChildren();

        if (_preferencesManager.Preferences is not { } prefs)
            return;

        var group = new ButtonGroup();

        // A slot can disappear while the window is open, so drop a stale selection.
        if (MoffSelectedSlot is { } selected && !prefs.Characters.ContainsKey(selected))
            MoffSelectedSlot = null;

        // Default to the character they picked in the lobby, not whichever slot enumerates first,
        // or joining would silently spawn someone else.
        if (MoffSelectedSlot == null && prefs.Characters.ContainsKey(prefs.SelectedCharacterIndex))
            MoffSelectedSlot = prefs.SelectedCharacterIndex;

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is not { } humanoid)
                continue;

            MoffSelectedSlot ??= slot;

            var button = new CharacterPickerButton(
                _prototypeManager,
                _moffPlayerManager,
                group,
                humanoid,
                slot == MoffSelectedSlot,
                simple: true);

            // Inactive characters can still be late joined with; the flag only governs round start.
            button.ModulateSelfOverride = _moffSelection.IsSlotEnabled(slot) ? null : Color.DarkGray;

            button.OnPressed += _ =>
            {
                MoffSelectedSlot = slot;
                RebuildUI();
            };

            _moffLayout.CharacterList.AddChild(button);
        }
    }

    private HumanoidCharacterProfile? GetMoffSelectedProfile()
    {
        if (MoffSelectedSlot is not { } slot)
            return null;

        return _preferencesManager.Preferences?.Characters.GetValueOrDefault(slot) as HumanoidCharacterProfile;
    }
}
