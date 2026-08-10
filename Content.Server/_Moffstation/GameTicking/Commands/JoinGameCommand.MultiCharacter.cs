using Content.Server._Moffstation.Station;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Robust.Shared.Console;
using Robust.Shared.Player;

// ReSharper disable once CheckNamespace // Moff - Adds to existing class in non-moff namespace
namespace Content.Server.GameTicking.Commands;

internal sealed partial class JoinGameCommand
{
    [Dependency] private IServerPreferencesManager _moffPreferences = default!;

    /// <summary>
    /// Validates the argument count and takes the optional leading character-slot argument, trimming
    /// it off <paramref name="args"/> so the rest of the command sees upstream's two-argument form.
    /// </summary>
    private static bool TryTakeMoffSlotArg(IConsoleShell shell, ref string[] args, out int? slot)
    {
        slot = null;

        if (args.Length is not (2 or 3))
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return false;
        }

        if (args.Length != 3)
            return true;

        if (!int.TryParse(args[0], out var parsed))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return false;
        }

        slot = parsed;
        args = args[1..];
        return true;
    }

    /// <summary>
    /// Pins the character in <paramref name="slot"/> as the one this player is late joining with.
    /// </summary>
    private bool TrySetMoffCharacter(IConsoleShell shell, ICommonSession player, int slot)
    {
        if (_moffPreferences.GetPreferencesOrNull(player.UserId) is not { } prefs ||
            !prefs.Characters.TryGetValue(slot, out var profile))
        {
            shell.WriteError(Loc.GetString("moff-join-game-no-character-in-slot", ("slot", slot)));
            return false;
        }

        _entManager.System<MoffCharacterPickerSystem>().SetExplicitChoice(player.UserId, humanoid);
        return true;
    }
}
