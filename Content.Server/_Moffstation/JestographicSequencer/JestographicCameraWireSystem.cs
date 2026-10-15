using Content.Server.Silicons.StationAi;
using Content.Server.SurveillanceCamera;
using Content.Server.Wires;
using Content.Shared._Moffstation.JestographicSequencer;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Server._Moffstation.JestographicSequencer;

public sealed partial class JestographicCameraWireSystem : EntitySystem
{
    [Dependency] private WiresSystem _wires = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceCameraComponent, JestographicCameraWireCutEvent>(OnCameraJested);
    }

    private void OnCameraJested(Entity<SurveillanceCameraComponent> ent, ref JestographicCameraWireCutEvent args)
    {
        var cutAny = false;
        foreach (var wire in _wires.TryGetWires<CameraMapVisibilityWireAction>(ent))
        {
            if (wire.IsCut || wire.Action is not { } action || !action.Cut(args.UserUid, wire))
                continue;

            wire.IsCut = true;
            action.Update(wire);
            cutAny = true;
        }

        foreach (var wire in _wires.TryGetWires<AiVisionWireAction>(ent))
        {
            if (wire.IsCut || wire.Action is not { } action || !action.Cut(args.UserUid, wire))
                continue;

            wire.IsCut = true;
            action.Update(wire);
            cutAny = true;
        }

        if (!cutAny)
            return;

        _wires.RefreshUserInterface(ent);
        args.Handled = true;
    }
}