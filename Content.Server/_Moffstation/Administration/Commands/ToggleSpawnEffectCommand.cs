using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleSpawnEffectCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _sysManager = default!;

    public override string Command => "togglespawneffect";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError("This command can only be run by a player.");
            return;
        }

        var spawnEffectSystem = _sysManager.GetEntitySystem<SpawnEffectSystem>();
        var userId = player.UserId;

        if (args.Length == 0)
        {
            spawnEffectSystem.TrySetEffect(userId, null);
            shell.WriteLine(Loc.GetString("command-togglespawneffect-disabled"));
            return;
        }

        var protoId = args[0];

        if (!spawnEffectSystem.TrySetEffect(userId, protoId))
        {
            shell.WriteError(Loc.GetString("command-togglespawneffect-error", ("protoId", protoId)));
            return;
        }
        shell.WriteLine(Loc.GetString("command-togglespawneffect-enabled", ("protoId", protoId)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var spawnEffectSystem = _sysManager.GetEntitySystem<SpawnEffectSystem>();
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(spawnEffectSystem.GetEffects(), "PrototypeID");
    }


}
