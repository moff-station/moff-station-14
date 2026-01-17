using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleSpawnEffectCommand : LocalizedCommands
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
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

        if (!_protoManager.HasIndex<EntityPrototype>(protoId))
        {
            shell.WriteError(Loc.GetString("command-togglespawneffect-error", ("protoId", protoId)));
            return;
        }

        spawnEffectSystem.TrySetEffect(userId, protoId);
        shell.WriteLine(Loc.GetString("command-togglespawneffect-enabled", ("protoId", protoId)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        // Filter for all "AdminInstantEffect" prototypes
        var options = GetEffects();
        return CompletionResult.FromHintOptions(options, "PrototypeID");
    }

    public IOrderedEnumerable<CompletionOption> GetEffects()
    {
       return  _protoManager.EnumeratePrototypes<EntityPrototype>()
            .Where(p => p.ID.StartsWith("AdminInstantEffect"))
            .Select(p => new CompletionOption(p.ID, p.Name))
            .OrderBy(o => o.Value);
    }
}
