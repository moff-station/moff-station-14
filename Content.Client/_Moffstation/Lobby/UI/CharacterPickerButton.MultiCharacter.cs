using Content.Client._Moffstation.Preferences;

// Partial of the upstream CharacterPickerButton, so it must sit in the upstream namespace.
namespace Content.Client.Lobby.UI;

public sealed partial class CharacterPickerButton
{
    /// <summary>Shows and wires up the "active" toggle for <paramref name="slot"/>.</summary>
    public void SetupMoffEnabled(int slot)
    {
        var selection = IoCManager.Resolve<MoffCharacterSelectionManager>();

        MoffEnabledCheckBox.Visible = true;
        MoffEnabledCheckBox.Pressed = selection.IsSlotEnabled(slot);

        MoffEnabledCheckBox.OnToggled += args =>
        {
            selection.SetCharacterEnabled(slot, args.Pressed);
            args.Event.Handle(); // Otherwise the click also selects this character.
        };
    }
}
