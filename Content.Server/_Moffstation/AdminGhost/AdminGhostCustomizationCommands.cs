using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Shared._Moffstation.AdminGhost;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Overlays;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._Moffstation.AdminGhost;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class GhostCustomCommand : LocalizedCommands
{
    [Dependency] private IEntitySystemManager _sysManager = default!;

    public override string Command => "ghostcustom";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Usage: ghostcustom <subcommand> [args...]");
            return;
        }

        var player = shell.Player;
        if (player?.AttachedEntity is not { Valid: true } attached)
        {
            shell.WriteError("You must have an entity to use this command.");
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        if (!entManager.TryGetComponent(attached, out GhostComponent? ghost) || !ghost.CanGhostInteract)
        {
            shell.WriteError("You must be an admin ghost to use this command.");
            return;
        }

        var system = _sysManager.GetEntitySystem<AdminGhostCustomizationSystem>();
        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args[1..];

        switch (subcommand)
        {
            case "sprite":
                HandleSprite(shell, attached, subArgs, system);
                break;
            case "name":
                HandleName(shell, attached, subArgs, system);
                break;
            case "desc":
                HandleDescription(shell, attached, subArgs, system);
                break;
            case "speed":
                HandleSpeed(shell, attached, subArgs, system);
                break;
            case "maptext":
                HandleMapText(shell, attached, subArgs, system);
                break;
            case "overlay":
                HandleOverlay(shell, attached, subArgs, system, entManager);
                break;
            case "save":
                await HandleSave(shell, attached, system);
                break;
            case "load":
                await HandleLoad(shell, attached, system);
                break;
            case "reset":
                system.ResetAll(attached);
                shell.WriteLine("All ghost customizations reset.");
                break;
            default:
                shell.WriteError($"Unknown subcommand '{subcommand}'.");
                shell.WriteError("Available: sprite, name, desc, speed, maptext, overlay, save, load, reset");
                break;
        }
    }

    private void HandleSprite(IConsoleShell shell, EntityUid uid, string[] args, AdminGhostCustomizationSystem system)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Usage: ghostcustom sprite <protoId|clear>");
            return;
        }

        if (args[0] == "clear")
        {
            system.SetSpritePrototype(uid, null);
            shell.WriteLine("Sprite reset to default.");
            return;
        }

        system.SetSpritePrototype(uid, args[0]);
        shell.WriteLine($"Sprite set to {args[0]}.");
    }

    private void HandleName(IConsoleShell shell, EntityUid uid, string[] args, AdminGhostCustomizationSystem system)
    {
        if (args.Length == 0)
        {
            system.SetCustomName(uid, null);
            shell.WriteLine("Name reset to default.");
            return;
        }

        var name = string.Join(" ", args);
        system.SetCustomName(uid, name);
        shell.WriteLine($"Name set to '{name}'.");
    }

    private void HandleDescription(IConsoleShell shell, EntityUid uid, string[] args, AdminGhostCustomizationSystem system)
    {
        if (args.Length == 0)
        {
            system.SetCustomDescription(uid, null);
            shell.WriteLine("Description reset to default.");
            return;
        }

        var desc = string.Join(" ", args);
        system.SetCustomDescription(uid, desc);
        shell.WriteLine($"Description set to '{desc}'.");
    }

    private void HandleSpeed(IConsoleShell shell, EntityUid uid, string[] args, AdminGhostCustomizationSystem system)
    {
        if (args.Length < 2)
        {
            shell.WriteError("Usage: ghostcustom speed <walkSpeed> <sprintSpeed>");
            return;
        }

        if (!float.TryParse(args[0], out var walk) || !float.TryParse(args[1], out var sprint))
        {
            shell.WriteError("Invalid speed values.");
            return;
        }

        system.SetWalkSpeed(uid, walk);
        system.SetSprintSpeed(uid, sprint);
        shell.WriteLine($"Speed set to walk={walk}, sprint={sprint}.");
    }

    private void HandleMapText(IConsoleShell shell, EntityUid uid, string[] args, AdminGhostCustomizationSystem system)
    {
        if (args.Length == 0 || args[0] == "clear")
        {
            system.SetMapText(uid, null);
            shell.WriteLine("Map text removed.");
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();
        var existing = entManager.GetComponentOrNull<AdminGhostCustomizationComponent>(uid)?.MapText;
        var data = new MapTextData
        {
            Color = existing?.Color ?? Color.White,
            FontSize = existing?.FontSize ?? 12,
            Offset = existing?.Offset ?? Vector2.Zero,
        };
        var textParts = new List<string>();
        var nextArgIsFlag = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (nextArgIsFlag)
            {
                nextArgIsFlag = false;
                continue;
            }

            switch (args[i])
            {
                case "--color" when i + 1 < args.Length && Color.TryFromHex(args[i + 1]) is { } color:
                    data.Color = color;
                    nextArgIsFlag = true;
                    break;
                case "--fontsize" when i + 1 < args.Length && int.TryParse(args[i + 1], out var fontSize):
                    data.FontSize = fontSize;
                    nextArgIsFlag = true;
                    break;
                case "--offset-x" when i + 1 < args.Length && float.TryParse(args[i + 1], out var offsetX):
                    data.Offset = new Vector2(offsetX, data.Offset.Y);
                    nextArgIsFlag = true;
                    break;
                case "--offset-y" when i + 1 < args.Length && float.TryParse(args[i + 1], out var offsetY):
                    data.Offset = new Vector2(data.Offset.X, offsetY);
                    nextArgIsFlag = true;
                    break;
                default:
                    textParts.Add(args[i]);
                    break;
            }
        }

        data.Text = string.Join(" ", textParts);
        system.SetMapText(uid, data);
        shell.WriteLine("Map text set.");
    }

    private void HandleOverlay(IConsoleShell shell, EntityUid uid, string[] args, AdminGhostCustomizationSystem system, IEntityManager entManager)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Usage: ghostcustom overlay <jobicons|criminal|mindshield|syndicate|healthbars> [on/off]");
            return;
        }

        var name = args[0].ToLowerInvariant();
        bool? enable = args.Length > 1 ? args[1] != "off" : null;

        switch (name)
        {
            case "jobicons":
                if (enable ?? !entManager.HasComponent<ShowJobIconsComponent>(uid))
                    system.ShowOverlay<ShowJobIconsComponent>(uid);
                else
                    system.HideOverlay<ShowJobIconsComponent>(uid);
                break;
            case "criminal":
                if (enable ?? !entManager.HasComponent<ShowCriminalRecordIconsComponent>(uid))
                    system.ShowOverlay<ShowCriminalRecordIconsComponent>(uid);
                else
                    system.HideOverlay<ShowCriminalRecordIconsComponent>(uid);
                break;
            case "mindshield":
                if (enable ?? !entManager.HasComponent<ShowMindShieldIconsComponent>(uid))
                    system.ShowOverlay<ShowMindShieldIconsComponent>(uid);
                else
                    system.HideOverlay<ShowMindShieldIconsComponent>(uid);
                break;
            case "syndicate":
                if (enable ?? !entManager.HasComponent<ShowSyndicateIconsComponent>(uid))
                    system.ShowOverlay<ShowSyndicateIconsComponent>(uid);
                else
                    system.HideOverlay<ShowSyndicateIconsComponent>(uid);
                break;
            case "healthbars":
                if (enable ?? !entManager.HasComponent<ShowHealthBarsComponent>(uid))
                    system.ShowOverlay<ShowHealthBarsComponent>(uid);
                else
                    system.HideOverlay<ShowHealthBarsComponent>(uid);
                break;
            default:
                shell.WriteError($"Unknown overlay '{name}'.");
                shell.WriteError("Available: jobicons, criminal, mindshield, syndicate, healthbars");
                return;
        }

        shell.WriteLine($"Overlay '{name}' toggled.");
    }

    private async Task HandleSave(IConsoleShell shell, EntityUid uid, AdminGhostCustomizationSystem system)
    {
        var saveManager = IoCManager.Resolve<AdminGhostSaveManager>();
        var result = await system.SaveCurrentToDb(uid, shell.Player!.UserId, saveManager);
        if (result)
            shell.WriteLine("Ghost customizations saved.");
        else
            shell.WriteError("Failed to save ghost customizations.");
    }

    private async Task HandleLoad(IConsoleShell shell, EntityUid uid, AdminGhostCustomizationSystem system)
    {
        var saveManager = IoCManager.Resolve<AdminGhostSaveManager>();
        var data = saveManager.GetData(shell.Player!.UserId);
        if (data == null)
        {
            shell.WriteLine("No saved ghost customizations found.");
            return;
        }

        system.ApplySavedData(uid, data);
        shell.WriteLine("Saved ghost customizations loaded.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                ["sprite", "name", "desc", "speed", "maptext", "overlay", "save", "load", "reset"],
                "subcommand"
            );
        }

        if (args.Length == 2)
        {
            return args[0].ToLowerInvariant() switch
            {
                "sprite" => CompletionResult.FromHint("protoId|clear"),
                "name" => CompletionResult.FromHint("name"),
                "desc" => CompletionResult.FromHint("description"),
                "speed" => CompletionResult.FromHint("walkSpeed sprintSpeed"),
                "maptext" => CompletionResult.FromHint("text [--color #hex] [--fontsize n] [--offset-x n] [--offset-y n]|clear"),
                "overlay" => CompletionResult.FromHintOptions(
                    ["jobicons", "criminal", "mindshield", "syndicate", "healthbars"],
                    "overlayName"
                ),
                _ => CompletionResult.Empty
            };
        }

        if (args.Length == 3 && args[0].ToLowerInvariant() == "overlay")
        {
            return CompletionResult.FromHintOptions(["on", "off"], "state");
        }

        return CompletionResult.Empty;
    }
}
