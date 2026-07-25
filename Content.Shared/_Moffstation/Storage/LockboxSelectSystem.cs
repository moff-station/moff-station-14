using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Storage;

public sealed partial class LockboxSelectSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnAfterUiOpen(Entity<LockboxSelectComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        var playerAccess = _access.FindAccessTags(args.User);
        var available = new List<EntProtoId>();

        foreach (var (access, proto) in ent.Comp.Options)
        {
            if (playerAccess.Contains(access))
                available.Add(proto);
        }

        if (available.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("lockbox-select-no-access"), ent.Owner, args.User, PopupType.SmallCaution);
            _ui.CloseUi(ent.Owner, LockboxSelectUiKey.Key, args.User);
            return;
        }

        _ui.SetUiState(ent.Owner, LockboxSelectUiKey.Key, new LockboxSelectBoundUserInterfaceState(available));
    }

    [SubscribeLocalEvent]
    private void OnDepartmentSelected(Entity<LockboxSelectComponent> ent, ref LockboxSelectMessage args)
    {
        var selected = args.SelectedProto;
        var playerAccess = _access.FindAccessTags(args.Actor);

        if (!ent.Comp.Options.Any(kvp => kvp.Value == selected && playerAccess.Contains(kvp.Key)))
            return;

        // Spawn the department lockbox in place and remove the generic one
        var xform = Transform(ent);
        var newEnt = PredictedSpawnAtPosition(selected, xform.Coordinates);
        _transform.SetLocalRotation(newEnt, xform.LocalRotation);

        QueueDel(ent.Owner);
    }
}
