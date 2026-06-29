using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Shared._Moffstation.Radio;

[AnyCommand]
public sealed partial class RadioWarpCommand : LocalizedEntityCommands
{
    [Dependency] private SharedRadioWarpSystem _radioWarp = default!;

    public const string CommandName = "radio_warp_to";

    public override string Command => CommandName;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || shell.Player is not { AttachedEntity: { } ent })
            return;

        var target = args[0];

        if (!NetEntity.TryParse(target, out var netTarget))
            return;

        _radioWarp.TryWarp(ent,  netTarget);
    }
}
