using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.Discord.Commands;

[AnyCommand]
public sealed partial class DiscordLinkCommand : LocalizedCommands
{
    [Dependency] private IServerDbManager _db = default!;

    private const string Set = "set";
    private const string Clear = "clear";

    public override string Command => "discordlink";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        args.TryFirstOrDefault(out var subcommand);
        await (subcommand switch
        {
            null => GetImpl(shell, player),
            Set => SetImpl(shell, args, player),
            Clear => ClearImpl(shell, player),
            _ => UnknownImpl(shell, subcommand),
        });
    }

    private Task UnknownImpl(IConsoleShell shell, string subcommand)
    {
        shell.WriteError(Loc.GetString("cmd-discordlink-unknown-subcommand", ("sub", subcommand)));
        return Task.CompletedTask;
    }

    private async Task GetImpl(IConsoleShell shell, ICommonSession player)
    {
        shell.WriteLine(
            await _db.GetDiscordId(player.UserId) is { } discordId
                ? Loc.GetString("cmd-discordlink-result", ("id", discordId))
                : Loc.GetString("cmd-discordlink-not-set")
        );
    }

    private async Task SetImpl(IConsoleShell shell, string[] args, ICommonSession player)
    {
#if FULL_RELEASE
        shell.WriteError(Loc.GetString("cmd-discordlink-disabled"));
        return;
#else
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-discordlink-set-usage"));
            return;
        }

        if (!await _db.SetDiscordId(player.UserId, args[1]))
        {
            shell.WriteError(Loc.GetString("cmd-discordlink-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-discordlink-set", ("id", args[1])));
#endif
    }

    private async Task ClearImpl(IConsoleShell shell, ICommonSession player)
    {
#if FULL_RELEASE
        shell.WriteError(Loc.GetString("cmd-discordlink-disabled"));
        return;
#else

        if (!await _db.SetDiscordId(player.UserId, null))
        {
            shell.WriteError(Loc.GetString("cmd-discordlink-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-discordlink-cleared"));
#endif
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
#if FULL_RELEASE
        return CompletionResult.Empty;
#else
        return args.Length == 1 ? CompletionResult.FromOptions([Set, Clear]) : CompletionResult.Empty;
#endif
    }
}
