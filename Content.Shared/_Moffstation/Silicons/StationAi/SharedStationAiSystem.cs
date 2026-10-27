using Content.Shared._Moffstation.Chitter;
using Content.Shared._Moffstation.Silicons.StationAi;

// ReSharper disable once CheckNamespace // Moffstation additions to the existing system.
namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    [SubscribeLocalEvent]
    private void OnAiShellControlStarted(Entity<StationAiHeldComponent> entity, ref AiShellControlStartedEvent args)
    {
        if (!TryGetCore(entity, out var core))
            return;

        SwitchRemoteEntityMode(core, false);
    }

    [SubscribeLocalEvent]
    private void OnAiShellControlStopped(Entity<StationAiHeldComponent> entity, ref AiShellControlStoppedEvent args)
    {
        if (TryGetCore(entity, out var core))
        {
            SwitchRemoteEntityMode(core, true);
            if (core.Comp?.RemoteEntity is { } remoteEnt)
                _xforms.SetCoordinates(remoteEnt, Transform(args.Shell).Coordinates);
        }
    }

    private static readonly LocId StationAiTitle = "job-name-station-ai";

    [SubscribeLocalEvent]
    private void On(Entity<StationAiHeldComponent> entity, ref ChitterGetIdentityInfoEvent args)
    {
        args.Name = Name(entity);
        args.Title = Loc.GetString(StationAiTitle);
    }
}
