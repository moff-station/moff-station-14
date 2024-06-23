using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._AXOLOTL;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server._AXOLOTL.RespawnButton;

[AnyCommand]
public sealed class GhostRespawnCommand : IConsoleCommand
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public string Command => "ghostrespawn";
    public string Description => "Allows the player to return to the lobby if they've been dead long enough, allowing re-entering the round as another character.";
    public string Help => $"{Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_configurationManager.GetCVar(AxolotlCVars.RespawnTime) == 0)
        {
            shell.WriteLine("Respawning is disabled, ask an admin to respawn you.");
            return;
        }

        if (_playerManager.PlayerCount > _configurationManager.GetCVar(AxolotlCVars.MaxPlayersForRespawnButton))
        {
            shell.WriteLine("Too many players online to be automatically respawn.");
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
        var time = (_gameTiming.CurTime - ghost.TimeOfDeath);
        var respawnTime = _configurationManager.GetCVar(AxolotlCVars.RespawnTime);

        if (respawnTime > time.TotalSeconds)
        {
            shell.WriteLine($"You haven't been dead long enough. You have been dead {time.TotalSeconds} seconds of the required {respawnTime}.");
            return;
        }

        var gameTicker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();

        gameTicker.Respawn(shell.Player);
    }
}
