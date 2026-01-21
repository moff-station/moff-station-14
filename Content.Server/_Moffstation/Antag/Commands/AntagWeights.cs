using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.Antag.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class AntagWeights : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly WeightedAntagManager _antagWeight = default!;

    public override string Command => "antagweights";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var users = _players.Sessions
            .OrderBy(c => _antagWeight.GetWeight(c.UserId))
            .ToArray();

        var total = users.Sum(user => _antagWeight.GetWeight(user.UserId));

        foreach (var user in users)
        {
            shell.WriteLine(Loc.GetString("cmd-antagweight-list",
                ("player", user.Name),
                ("weight", _antagWeight.GetWeight(user.UserId)),
                ("percent", _antagWeight.GetWeight(user.UserId) / total * 100)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHint(Loc.GetString("cmd-antagweight-completion"));
    }
}
