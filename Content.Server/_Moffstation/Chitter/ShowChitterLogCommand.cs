using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.Chitter;

[AdminCommand(AdminFlags.Logs)]
public sealed partial class ShowChitterLogCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entManager = default!;

    public override string Command => "showchitterlog";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _entManager.System<ChitterLogEuiSystem>().Open(player);
    }
}
