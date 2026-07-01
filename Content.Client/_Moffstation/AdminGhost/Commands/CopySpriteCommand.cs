using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client._Moffstation.AdminGhost.Commands;

public sealed class CopySpriteCommand : IConsoleCommand
{
    public string Command => "copysprite";
    public string Description => "Copies the SpriteComponent from one entity to another.";
    public string Help => "copysprite <sourceUid> <targetUid>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Usage: copysprite <sourceUid> <targetUid>");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var sourceUid))
        {
            shell.WriteError($"Could not parse '{args[0]}' as an entity UID.");
            return;
        }

        if (!EntityUid.TryParse(args[1], out var targetUid))
        {
            shell.WriteError($"Could not parse '{args[1]}' as an entity UID.");
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();

        if (!entManager.TryGetComponent(sourceUid, out SpriteComponent? sourceSprite))
        {
            shell.WriteError($"Entity {sourceUid} has no SpriteComponent.");
            return;
        }

        if (!entManager.TryGetComponent(targetUid, out SpriteComponent? targetSprite))
        {
            shell.WriteError($"Entity {targetUid} has no SpriteComponent.");
            return;
        }

        var spriteSystem = entManager.System<SpriteSystem>();
        spriteSystem.CopySprite((sourceUid, sourceSprite), (targetUid, targetSprite));
        shell.WriteLine($"Sprite copied from {sourceUid} to {targetUid}.");
    }
}
