using System.Numerics;
using Content.Client._Moffstation.Preferences;
using Content.Client.Lobby.UI;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

// Partial of the upstream LateJoinGui, so it must sit in the upstream namespace.
namespace Content.Client.LateJoin;

public sealed partial class LateJoinGui
{
    [Dependency] private readonly ISharedPlayerManager _moffPlayerManager = default!;
    [Dependency] private readonly MoffCharacterSelectionManager _moffSelection = default!;

    private BoxContainer _moffCharacterList = default!;

    /// <summary>Which character slot the player is joining as, if they have one.</summary>
    private int? MoffSelectedSlot { get; set; }

    /// <summary>
    /// Wraps the upstream job list in a two column layout with a character picker on the left.
    /// </summary>
    private Control BuildMoffLayout(Control jobList)
    {
        _moffCharacterList = new BoxContainer { Orientation = LayoutOrientation.Vertical };

        return new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            Children =
            {
                new ScrollContainer
                {
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = 1,
                    Children = { _moffCharacterList },
                },
                new PanelContainer
                {
                    MinSize = new Vector2(2, 0),
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = StyleNano.NanoGold,
                        ContentMarginTopOverride = 2,
                    },
                },
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    VerticalExpand = true,
                    HorizontalExpand = true,
                    SizeFlagsStretchRatio = 1.3f,
                    Margin = new Thickness(5, 5, 0, 0),
                    Children = { jobList },
                },
            },
        };
    }

    private void RebuildMoffCharacterList()
    {
        _moffCharacterList.RemoveAllChildren();

        if (_preferencesManager.Preferences is not { } prefs)
            return;

        var group = new ButtonGroup();

        // A slot can disappear while the window is open, so fall back to the first one.
        if (MoffSelectedSlot is { } selected && !prefs.Characters.ContainsKey(selected))
            MoffSelectedSlot = null;

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile is not HumanoidCharacterProfile humanoid)
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

            _moffCharacterList.AddChild(button);
        }
    }

    private HumanoidCharacterProfile? GetMoffSelectedProfile()
    {
        if (MoffSelectedSlot is not { } slot)
            return null;

        return _preferencesManager.Preferences?.Characters.GetValueOrDefault(slot) as HumanoidCharacterProfile;
    }
}
