using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Client.UserInterface;
using Robust.Shared.Console;

namespace Content.Client._Moffstation.AdminGhost.Commands;

public sealed class OpenAdminGhostUICommand : IConsoleCommand
{
    public string Command => "ghostui";
    public string Description => "Opens the Admin Ghost Customization window.";
    public string Help => "ghostui";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity is not { Valid: true } attached)
        {
            shell.WriteError("You must have an entity to use this command.");
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        if (!entManager.HasComponent<GhostComponent>(attached))
        {
            shell.WriteError("You must be a ghost to use this command.");
            return;
        }

        var window = new AdminGhostWindow();
        window.Initialize(attached);
        window.OpenCentered();
    }
}
