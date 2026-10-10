using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;

// ReSharper disable once CheckNamespace // Moff - Adds to existing class in non-moff namespace
namespace Content.Server.GameTicking.Commands;

internal sealed partial class JoinGameCommand
{
    /// <summary>
    /// Validates the argument count and takes the optional leading character-slot argument, trimming
    /// it off <paramref name="args"/> so the rest of the command sees upstream's two-argument form.
    /// </summary>
    private static bool TryTakeMoffSlotArg(IConsoleShell shell, ref string[] args, [NotNullWhen(true)] out int? slot)
    {
        slot = null;

        if (args.Length is not (2 or 3))
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return false;
        }

        if (args.Length != 3)
            return false;

        if (!int.TryParse(args[0], out var parsed))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
            return false;
        }

        slot = parsed;
        args = args[1..];
        return true;
    }
}
