using Content.Shared._Moffstation.Silicons.StationAi;

// ReSharper disable once CheckNamespace // Moffstation additions to the existing system.
namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    private void InitMoffstation()
    {
        SubscribeLocalEvent<StationAiHeldComponent, AiShellControlStartedEvent>(OnAiShellControlStarted);
        SubscribeLocalEvent<StationAiHeldComponent, AiShellControlStoppedEvent>(OnAiShellControlStopped);
    }

    private void OnAiShellControlStarted(Entity<StationAiHeldComponent> entity, ref AiShellControlStartedEvent args)
    {
        if (!TryGetCore(entity, out var core))
            return;

        SwitchRemoteEntityMode(core, false);
    }

    private void OnAiShellControlStopped(Entity<StationAiHeldComponent> entity, ref AiShellControlStoppedEvent args)
    {
        if (TryGetCore(entity, out var core))
        {
            SwitchRemoteEntityMode(core, true);
            if (core.Comp?.RemoteEntity is { } remoteEnt)
                _xforms.SetCoordinates(remoteEnt, Transform(args.Shell).Coordinates);
        }
    }
}
