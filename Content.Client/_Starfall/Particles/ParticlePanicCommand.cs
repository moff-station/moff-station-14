using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Client._Starfall.Particles;

/// <summary>
/// Immediately kills all active particle emitters and their live particles.
/// Useful if something goes wrong and needs to be killed FAST..
/// </summary>
[AnyCommand]
public sealed partial class ParticlePanicCommand : LocalizedEntityCommands
{
    [Dependency] private ParticleSystem _particles = default!;

    public override string Command => "particlepanic";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var count = _particles.KillAll();
        shell.WriteLine(Loc.GetString("cmd-particlepanic-cleared", ("count", count)));
    }
}
