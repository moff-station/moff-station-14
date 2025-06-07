using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands;

[AnyCommand]
internal sealed class CollectiveMindChatCommand : IConsoleCommand
{
    public string Command => "cmsay";
    public string Description => "Send chat messages to the collective mind.";
    public string Help => "cmsay <text>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not {} playerEntity)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        IoCManager.Resolve<IEntitySystemManager>()
                .GetEntitySystem<ChatSystem>()
                .TrySendInGameICMessage(
                    playerEntity,
                    message,
                    InGameICChatType.CollectiveMind,
                    ChatTransmitRange.Normal
                );
    }
}

