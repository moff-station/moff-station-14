using System.Numerics;
using Content.Client._Moffstation.Preferences;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class CharacterPickerButton
{
    private const string EnabledLoc = "moff-character-enabled-button";
    private const string DisabledLoc = "moff-character-disabled-label";

    private const string OutlineTexture = "/Textures/Interface/Nano/slider_outline.svg.96dpi.png";

    // Built in code rather than XAML so the upstream .xaml file needs no Moff edit. Null when the
    // button was built "simple", i.e. for the late join picker.
    private Button? _moffEnabledCheck;

    /// <summary>
    /// A character no longer ranks its own jobs, so the subtitle shows whichever of its jobs the
    /// player rates highest.
    /// </summary>
    private static string BuildMoffDescription(HumanoidCharacterProfile profile, IPrototypeManager protoMan)
    {
        var best = IoCManager.Resolve<MoffCharacterSelectionManager>().GetPreferredJob(profile);

        if (best == null || !protoMan.TryIndex(best.Value, out var jobProto))
            return profile.Name;

        return $"{profile.Name}\n{jobProto.LocalizedName}";
    }

    /// <summary>An outlined panel, matching the look of the sliders elsewhere in the editor.</summary>
    private PanelContainer MakeMoffOutline()
    {
        var styleBox = new StyleBoxTexture
        {
            Texture = Theme.ResolveTexture(OutlineTexture),
            TextureScale = new Vector2(1.1f),
            Modulate = StyleNano.PanelDark,
        };

        styleBox.SetPatchMargin(StyleBox.Margin.All, 12);
        styleBox.SetContentMarginOverride(StyleBox.Margin.All, 0);
        styleBox.SetExpandMargin(StyleBox.Margin.All, 1);

        return new PanelContainer { PanelOverride = styleBox };
    }

    /// <summary>
    /// Builds the enable/delete column and wires delete. <paramref name="simple"/> omits it, for the
    /// late join picker which only needs the portrait and name.
    /// </summary>
    private void SetupMoffButtons(bool isSelected, bool simple)
    {
        // Upstream's own delete pair is superseded by the column below.
        DeleteButton.Visible = false;
        ConfirmDeleteButton.Visible = false;

        HorizontalExpand = true;

        if (simple)
            return;

        var divider = new PanelContainer
        {
            MinSize = new Vector2(2, 0),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = StyleNano.NanoGold,
                ContentMarginTopOverride = 2,
            },
        };

        var enabledCheck = new Button
        {
            ToggleMode = true,
            HorizontalAlignment = HAlignment.Stretch,
            ToolTip = Loc.GetString("moff-character-enabled-button-tooltip"),
            Text = Loc.GetString(EnabledLoc),
        };

        _moffEnabledCheck = enabledCheck;

        var enabledOutline = MakeMoffOutline();
        enabledOutline.AddChild(enabledCheck);

        var deleteButton = new ConfirmButton
        {
            HorizontalAlignment = HAlignment.Stretch,
            Text = Loc.GetString("character-setup-gui-character-picker-button-delete-button"),
        };

        var deleteOutline = MakeMoffOutline();
        deleteOutline.AddChild(deleteButton);
        deleteOutline.Visible = !isSelected;

        var buttonBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalAlignment = VAlignment.Center,
            VerticalExpand = true,
            SeparationOverride = 4,
            SetWidth = 80,
            Margin = new Thickness(8, 0, 0, 0),
            Children = { enabledOutline, deleteOutline },
        };

        InternalHBox.AddChild(divider);
        InternalHBox.AddChild(buttonBox);

        AddStyleClass(StyleNano.ButtonOpenRight);

        deleteButton.OnPressed += _ => OnDeletePressed?.Invoke();
    }

    /// <summary>Shows and wires up the "active" toggle for <paramref name="slot"/>.</summary>
    public void SetupMoffEnabled(int slot)
    {
        // The late join picker builds these buttons simple, so there is nothing to wire.
        if (_moffEnabledCheck is not { } check)
            return;

        var selection = IoCManager.Resolve<MoffCharacterSelectionManager>();

        SetMoffEnabledVisuals(check, selection.IsSlotEnabled(slot));

        check.OnToggled += args =>
        {
            selection.SetCharacterEnabled(slot, args.Pressed);
            SetMoffEnabledVisuals(check, args.Pressed);
            OnEnableToggled?.Invoke(args.Pressed);
            args.Event.Handle(); // Otherwise the click also selects this character.
        };
    }

    private static void SetMoffEnabledVisuals(Button check, bool enabled)
    {
        check.Pressed = enabled;
        check.Text = Loc.GetString(enabled ? EnabledLoc : DisabledLoc);
    }
}
