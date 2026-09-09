using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.Chitter.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed partial class OpenChitterLogCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euis = default!;

    public override string Command => "showchitterlog";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("cmd-showchitterlog-server"));
            return;
        }

        var ui = new ChitterLogEui();
        _euis.OpenEui(ui, admin);
    }
}
