using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleSpawnEffectCommand : LocalizedCommands
{
    [Dependency] private readonly SpawnEffectSystem _spawnEffSys = default!;

    public override string Command => "togglespawneffect";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError("This command can only be run by a player.");
            return;
        }

        var userId = player.UserId;

        if (args.Length == 0)
        {
            _spawnEffSys.TrySetEffect(userId, null);
            shell.WriteLine(Loc.GetString("command-togglespawneffect-disabled"));
            return;
        }

        var protoId = args[0];

        if (!_spawnEffSys.TrySetEffect(userId, protoId))
        {
            shell.WriteLine(Loc.GetString("command-togglespawneffect-enabled", ("protoId", protoId)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;
        return CompletionResult.FromHintOptions(_spawnEffSys.GetEffects(), "PrototypeID");
    }
}
