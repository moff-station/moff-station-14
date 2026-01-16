using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleSpawnEffectCommand : IConsoleCommand
{
    public string Command => "togglespawneffect";
    public string Description => Loc.GetString("command-togglespawneffect-description");
    public string Help => Loc.GetString("command-togglespawneffect-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpawnEffectSystem>();

        if (args.Length == 0 || args[0].ToLower() == "off")
        {
            system.ActiveEffect = null;
            shell.WriteLine(Loc.GetString("command-togglespawneffect-disabled"));
            return;
        }

        var protoId = args[0];
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        if (!protoManager.HasIndex<EntityPrototype>(protoId))
        {
            shell.WriteError(Loc.GetString("command-togglespawneffect-error", ("protoId", protoId)));
            return;
        }

        system.ActiveEffect = protoId;
        shell.WriteLine(Loc.GetString("command-togglespawneffect-enabled", ("protoId", protoId)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        //Fillter for all "AdminInstantEffect" prototypes
        var options = protoManager.EnumeratePrototypes<EntityPrototype>()
            .Where(p => p.ID.StartsWith("AdminInstantEffect"))
            .Select(p => new CompletionOption(p.ID, p.Name))
            .Append(new CompletionOption("off", Loc.GetString("command-togglespawneffect-option-off")))
            .OrderBy(o => o.Value);

        return CompletionResult.FromHintOptions(options, "PrototypeID");
    }
}
