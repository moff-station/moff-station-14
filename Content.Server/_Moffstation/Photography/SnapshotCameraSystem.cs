using Content.Server.Movement.Components;
using Content.Shared._Moffstation.Photography;
using Content.Shared.Camera;
using Content.Shared.Hands;

namespace Content.Server._Moffstation.Photography;

public sealed partial class SnapshotCameraSystem : SharedSnapshotCameraSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnapshotCameraComponent, HeldRelayedEvent<GetEyePvsScaleRelayedEvent>>(OnGetPvsScale);
    }

    /// <summary>
    /// The viewfinder's extended view needs a larger PVS range, like binoculars.
    /// </summary>
    private void OnGetPvsScale(Entity<SnapshotCameraComponent> ent, ref HeldRelayedEvent<GetEyePvsScaleRelayedEvent> args)
    {
        if (ent.Comp.Aiming && TryComp<EyeCursorOffsetComponent>(ent, out var cursorOffset))
            args.Args.Scale += cursorOffset.PvsIncrease;
    }
}
