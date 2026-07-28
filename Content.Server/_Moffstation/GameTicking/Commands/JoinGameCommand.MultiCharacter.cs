using Content.Server._Moffstation.Station;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Robust.Shared.Console;
using Robust.Shared.Player;

// Partial of the upstream JoinGameCommand, so it must sit in the upstream namespace.
namespace Content.Server.GameTicking.Commands;

sealed partial class JoinGameCommand
{
    [Dependency] private readonly IServerPreferencesManager _moffPreferences = default!;

    /// <summary>
    /// Pins the character in <paramref name="slot"/> as the one this player is late joining with.
    /// </summary>
    private bool TrySetMoffCharacter(IConsoleShell shell, ICommonSession player, int slot)
    {
        var prefs = _moffPreferences.GetPreferences(player.UserId);

        if (!prefs.Characters.TryGetValue(slot, out var profile) || profile is not HumanoidCharacterProfile humanoid)
        {
            shell.WriteError(Loc.GetString("moff-join-game-no-character-in-slot", ("slot", slot)));
            return false;
        }

        _entManager.System<MoffCharacterPickerSystem>().SetExplicitChoice(player.UserId, humanoid);
        return true;
    }
}
