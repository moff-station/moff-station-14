using System.Numerics;
using Content.Client._Moffstation.Preferences;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

// Partial of the upstream CharacterPickerButton, so it must sit in the upstream namespace.
namespace Content.Client.Lobby.UI;

public sealed partial class CharacterPickerButton
{
    private const string EnabledLoc = "moff-character-enabled-button";
    private const string DisabledLoc = "moff-character-disabled-label";

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

    /// <summary>Styles the side column and wires delete. <paramref name="simple"/> hides it entirely.</summary>
    private void SetupMoffButtons(bool isSelected, bool simple)
    {
        foreach (var panel in new[] { EnabledCheckOutline, DeleteButtonOutline })
        {
            if (panel.PanelOverride is not StyleBoxTexture styleBox)
                continue;

            styleBox.Texture = Theme.ResolveTexture("/Textures/Interface/Nano/slider_outline.svg.96dpi.png");
            styleBox.SetPatchMargin(StyleBox.Margin.All, 12);
            styleBox.SetContentMarginOverride(StyleBox.Margin.All, 0);
            styleBox.SetExpandMargin(StyleBox.Margin.All, 1);
            styleBox.TextureScale = new Vector2(1.1f);
            styleBox.Modulate = StyleNano.PanelDark;
        }

        if (simple)
        {
            ButtonDivider.Visible = false;
            ButtonBox.Visible = false;
            return;
        }

        AddStyleClass(StyleNano.ButtonOpenRight);
        DeleteButtonOutline.Visible = !isSelected;

        DeleteButton.OnPressed += _ => OnDeletePressed?.Invoke();
    }

    /// <summary>Shows and wires up the "active" toggle for <paramref name="slot"/>.</summary>
    public void SetupMoffEnabled(int slot)
    {
        var selection = IoCManager.Resolve<MoffCharacterSelectionManager>();

        SetMoffEnabledVisuals(selection.IsSlotEnabled(slot));

        EnabledCheck.OnToggled += args =>
        {
            selection.SetCharacterEnabled(slot, args.Pressed);
            SetMoffEnabledVisuals(args.Pressed);
            OnEnableToggled?.Invoke(args.Pressed);
            args.Event.Handle(); // Otherwise the click also selects this character.
        };
    }

    private void SetMoffEnabledVisuals(bool enabled)
    {
        EnabledCheck.Pressed = enabled;
        EnabledCheck.Text = Loc.GetString(enabled ? EnabledLoc : DisabledLoc);
    }
}
