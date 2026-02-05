using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._Moffstation.Antag.Commands;

[AdminCommand(AdminFlags.Admin)]
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

        float total = users.Sum(user => _antagWeight.GetWeight(user.UserId));

        foreach (var user in users)
        {
            shell.WriteLine(Loc.GetString("cmd-antagweight-list",
                ("player", user.Name),
                ("weight", _antagWeight.GetWeight(user.UserId)),
                ("percent", Math.Floor(_antagWeight.GetWeight(user.UserId) / total * 100f))));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.FromHint(Loc.GetString("cmd-antagweight-completion"));
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class SetAntagWeight : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly WeightedAntagManager _antagWeight = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override string Command => "setantagweight";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
            return;
        }
        var name = args[0];
        var weight = args[1];

        if (_players.TryGetSessionByUsername(name, out var target) && int.TryParse(weight, out var targetWeight))
        {
            var oldWeight = _antagWeight.GetWeight(target.UserId);
            _antagWeight.SetWeight(target.UserId, targetWeight);

            shell.WriteLine(Loc.GetString("cmd-antagweight-set",
                ("player", target.Name),
                ("old", oldWeight),
                ("new", targetWeight)));
            _adminLogger.Add(LogType.AdminCommands,
                LogImpact.Extreme,
                $"User {shell.Player} changed antag weights for player: {target.Name} ({oldWeight} -> {targetWeight})");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length < 2)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, "<PlayerIndex>");
        }

        return CompletionResult.Empty;
    }
}
