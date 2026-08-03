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
public sealed partial class GhostRespawnCommand : LocalizedEntityCommands
{
    public const string CommandName = "ghostrespawn";

    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    public override string Command => CommandName;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_configurationManager.GetCVar(MoffCCVars.RespawningEnabled))
        {
            shell.WriteLine(Loc.GetString("cmd-ghostrespawn-error-disabled"));
            return;
        }

        if (shell.Player is null)
        {
            shell.WriteLine(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (shell.Player.AttachedEntity is null)
        {
            shell.WriteLine(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (!_entityManager.TryGetComponent<GhostComponent>(shell.Player.AttachedEntity, out var ghost))
        {
            shell.WriteLine(Loc.GetString("cmd-ghostrespawn-error-not-ghost"));
            return;
        }

        var mindSystem = _entityManager.EntitySysManager.GetEntitySystem<MindSystem>();
        if (!mindSystem.TryGetMind(shell.Player.UserId, out _, out _))
        {
            shell.WriteLine(Loc.GetString("cmd-ghostrespawn-error-no-mind"));
            return;
        }

        var respawnDelay = TimeSpan.FromSeconds(_configurationManager.GetCVar(MoffCCVars.RespawnTime));
        var serverTime = _gameTiming.RealTime;
        var remainingTime = ghost.TimeOfDeath + respawnDelay - serverTime;

        if (remainingTime > TimeSpan.Zero)
        {
            shell.WriteLine(
                Loc.GetString(
                    "cmd-ghostrespawn-error-too-soon",
                    ("timeElapsed", double.Round((serverTime - ghost.TimeOfDeath).TotalSeconds)),
                    ("timeRequired", double.Round(respawnDelay.TotalSeconds))
                )
            );
            return;
        }

        _adminLogger.Add(
            LogType.Identity,
            LogImpact.High,
            $"{shell.Player.AttachedEntity} used ghost respawn button"
        );

        var gameTicker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();
        gameTicker.Respawn(shell.Player);
    }
}
