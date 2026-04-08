using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Body;
using Content.Server.Popups;
using Content.Shared._Moffstation.Geras;
using Content.Shared.Humanoid;
using Robust.Shared.Player;

namespace Content.Server._Moffstation.Geras;

/// <inheritdoc/>
public sealed class GerasSystem : SharedGerasSystem
{
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly VisualBodySystem _bodySystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // try to add geras action
        _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return; // i hate zomber.

        if (_polymorphSystem.PolymorphEntity(uid, component.GerasPolymorphId) is not { } ent)
            return;

        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
        {
            _bodySystem.ApplyProfile(ent, new() { SkinColor = profile.SkinColor });
        }

        _popupSystem.PopupPredicted(
            Loc.GetString("geras-popup-morph-message-user"),
            Loc.GetString("geras-popup-morph-message-others", ("entity", ent)),
            ent,
            ent
        );

        args.Handled = true;
    }
}
