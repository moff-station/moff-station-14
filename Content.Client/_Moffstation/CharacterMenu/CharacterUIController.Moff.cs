namespace Content.Client.UserInterface.Systems.Character;

public sealed partial class CharacterUIController
{
    public void OpenWindow()
    {
        if (_window == null)
            return;

        _characterInfo.RequestCharacterInfo();

        if (_window.IsOpen)
            return;

        CharacterButton?.SetClickPressed(true);
        _window.Open();
    }
}
