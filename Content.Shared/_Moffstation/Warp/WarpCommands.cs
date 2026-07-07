using Content.Shared._Moffstation.Radio;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Shared._Moffstation.Warp;


[AnyCommand]
public sealed partial class WarpToEntityCommand : LocalizedEntityCommands
{
    [Dependency] private SharedWarpSystem _warp = default!;

    public const string CommandName = "warp_to_entity";

    public override string Command => CommandName;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || shell.Player is not { AttachedEntity: { } ent })
            return;

        var target = args[0];

        if (!NetEntity.TryParse(target, out var netTarget))
            return;

        _warp.RequestWarpToEntity(netTarget);
    }
}
