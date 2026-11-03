using Content.Shared._Moffstation.Silicons.StationAi;
using Content.Shared.StationAi;

// ReSharper disable once CheckNamespace // Moffstation additions to the existing system.
namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    private void InitMoffstation()
    {
        SubscribeLocalEvent<StationAiHeldComponent, AiShellControlStartedEvent>(OnAiShellControlStarted);
        SubscribeLocalEvent<StationAiHeldComponent, AiShellControlStoppedEvent>(OnAiShellControlStopped);

        SubscribeLocalEvent<StationAiWhitelistComponent, ComponentInit>(OnWhitelistInit);
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

    // ensure that all machines whitelisted for the AI are visible for the AI.
    private void OnWhitelistInit(Entity<StationAiWhitelistComponent> ent, ref ComponentInit args)
    {
        if (TryComp<StationAiVisionComponent>(ent.Owner, out var vision))
            return;

        vision = EnsureComp<StationAiVisionComponent>(ent.Owner);
        vision.Range = 0.0f;
        Dirty(ent, vision);
    }
}
