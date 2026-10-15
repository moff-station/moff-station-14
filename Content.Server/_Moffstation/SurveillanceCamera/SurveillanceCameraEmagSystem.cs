using Content.Server.Silicons.StationAi;
using Content.Server.SurveillanceCamera;
using Content.Server.Wires;
using Content.Shared.Emag.Systems;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Server._Moffstation.SurveillanceCamera;

public sealed partial class SurveillanceCameraEmagSystem : EntitySystem
{
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private WiresSystem _wires = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceCameraComponent, GotEmaggedEvent>(OnCameraEmagged);
    }

    private void OnCameraEmagged(Entity<SurveillanceCameraComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        var cutAny = CutWires<CameraMapVisibilityWireAction>(ent, args.UserUid);
        cutAny |= CutWires<AiVisionWireAction>(ent, args.UserUid);

        if (!cutAny)
            return;

        _wires.RefreshUserInterface(ent);
        args.Handled = true;
        args.Repeatable = true;
    }

    private bool CutWires<TAction>(Entity<SurveillanceCameraComponent> camera, EntityUid user)
        where TAction : IWireAction
    {
        var cutAny = false;

        foreach (var wire in _wires.TryGetWires<TAction>(camera))
        {
            if (wire.IsCut || wire.Action is not { } action || !action.Cut(user, wire))
                continue;

            wire.IsCut = true;
            action.Update(wire);
            cutAny = true;
        }

        return cutAny;
    }
}