using Content.Client.Playtime;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Client.Commands;

[UsedImplicitly]
public sealed class PlaytimeNoticeCommand : LocalizedCommands
{
    [Dependency] private readonly ClientsidePlaytimeTrackingManager _playtime = default!;

    public override string Command => "playtime_notice";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _playtime.TriggerHourlyNotice();
        shell.WriteLine("Playtime notice posted.");
    }
}

