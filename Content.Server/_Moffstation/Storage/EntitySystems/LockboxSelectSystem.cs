using Content.Server.Popups;
using Content.Shared._Moffstation.Storage;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Storage.EntitySystems;

public sealed partial class LockboxSelectSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockboxSelectComponent, AfterActivatableUIOpenEvent>(OnAfterUiOpen);
        SubscribeLocalEvent<LockboxSelectComponent, LockboxSelectMessage>(OnDepartmentSelected);
    }

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

    private void OnDepartmentSelected(Entity<LockboxSelectComponent> ent, ref LockboxSelectMessage args)
    {
        var selected = args.SelectedProto;

        if (!ent.Comp.Options.ContainsValue(selected))
            return;

        var playerAccess = _access.FindAccessTags(args.Actor);
        var hasAccess = false;
        foreach (var (access, proto) in ent.Comp.Options)
        {
            if (proto == selected && playerAccess.Contains(access))
            {
                hasAccess = true;
                break;
            }
        }

        if (!hasAccess)
            return;

        // Spawn the department lockbox in place and remove the generic one
        var xform = Transform(ent);
        var newEnt = Spawn(selected, xform.Coordinates);
        _transform.SetLocalRotation(newEnt, xform.LocalRotation);

        QueueDel(ent.Owner);
    }
}
