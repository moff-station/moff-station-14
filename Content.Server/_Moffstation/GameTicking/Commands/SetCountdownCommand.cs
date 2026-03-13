using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.GameTicking.Commands;

[AdminCommand(AdminFlags.Round)]
public sealed class SetCountdownCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override string Command => "setcountdown";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            shell.WriteLine(Loc.GetString("shell-can-only-run-from-pre-round-lobby"));
            return;
        }

        if (!uint.TryParse(args[0], out var seconds) || seconds == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-setcountdown-invalid-seconds", ("value", args[0])));
            return;
        }

        var time = TimeSpan.FromSeconds(seconds);
        if (!_gameTicker.SetCountdown(time))
        {
            shell.WriteLine(Loc.GetString("cmd-setcountdown-too-late"));
        }
    }
}
