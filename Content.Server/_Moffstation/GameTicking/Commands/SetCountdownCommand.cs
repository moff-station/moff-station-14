using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class SetCountdownCommand : LocalizedCommands
{
    public override string Command => "setcountdown";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var gameTicker = IoCManager.Resolve<GameTicker>();

        if (gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteLine(Loc.GetString("shell-can-only-run-from-pre-round-lobby"));
            return;
        }

        if (args.Length < 1 || !uint.TryParse(args[0], out var seconds) || seconds == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-setcountdown-invalid-seconds", ("value", args.Length > 0 ? args[0] : "none")));
            return;
        }

        var time = TimeSpan.FromSeconds(seconds);
        if (!gameTicker.SetCountdown(time))
        {
            shell.WriteLine(Loc.GetString("cmd-setcountdown-too-late"));
        }
    }
}
