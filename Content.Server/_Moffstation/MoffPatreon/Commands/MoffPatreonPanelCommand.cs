using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.MoffPatreon.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class MoffPatreonPanelCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euis = default!;

    public override string Command => "moffpatreonpanel";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("cmd-moffpatreonpanel-server"));
            return;
        }

        var ui = new MoffPatreonEui();
        _euis.OpenEui(ui, admin);
    }
}
