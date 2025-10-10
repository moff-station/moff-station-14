using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.RespawnButton.Commands;

[AnyCommand]
public sealed class GhostRespawnCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;


    public override string Command => "ghostrespawn";
    public override string Description => "Allows the player to return to the lobby if they've been dead long enough, allowing re-entering the round as another character.";
    public override string Help => $"{Command}";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_configurationManager.GetCVar(MoffCCVars.RespawningEnabled))
        {
            shell.WriteLine("Respawning is disabled, ask an admin to respawn you.");
            return;
        }

        if (shell.Player is null)
        {
            shell.WriteLine("You cannot run this from the console!");
            return;
        }

        if (shell.Player.AttachedEntity is null)
        {
            shell.WriteLine("You cannot run this in the lobby, or without an entity.");
            return;
        }

        if (!_entityManager.TryGetComponent<GhostComponent>(shell.Player.AttachedEntity, out var ghost))
        {
            shell.WriteLine("You are not a ghost.");
            return;
        }

        var mindSystem = _entityManager.EntitySysManager.GetEntitySystem<MindSystem>();
        if (!mindSystem.TryGetMind(shell.Player.UserId, out _, out _))
        {
            shell.WriteLine("You have no mind.");
            return;
        }

        var respawnTime = TimeSpan.FromSeconds(_configurationManager.GetCVar(MoffCCVars.RespawnTime));
        var timeDead = _gameTiming.CurTime - ghost.TimeOfDeath;
        if (timeDead < respawnTime)
        {
            shell.WriteLine($"You haven't been dead long enough. You've been dead {timeDead.TotalSeconds:F0} seconds of the required {respawnTime.TotalSeconds:F0}.");
            return;
        }

        var gameTicker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();

        _adminLogger.Add(LogType.Identity,
            LogImpact.High,
            $"{_entityManager.ToPrettyString(shell.Player.AttachedEntity):Player} used ghost respawn button");
        gameTicker.Respawn(shell.Player);
    }
}
