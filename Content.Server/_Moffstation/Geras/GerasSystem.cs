using System.Diagnostics.Tracing;
using System.Linq;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Body;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Shared._Moffstation.Geras;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server._Moffstation.Geras;

/// <summary>
/// Geras is the god of old age, and A geras is the small morph of a slime. This system allows the slimes to have the morphing action.
/// </summary>
public sealed class GerasSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly VisualBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implantSystem = default!;

    public EntityUid? PausedMap { get; private set; }

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, ComponentShutdown>(OnRemoveGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
        SubscribeLocalEvent<GerasComponent, GerasVisualInitEvent>(OnGerasVisualInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        Del(PausedMap.Value);
    }

    /// <summary>
    /// Used internally to ensure a paused map that stores inactive forms.
    /// </summary>
    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        var mapUid = _map.CreateMap();
        _metaData.SetEntityName(mapUid, Loc.GetString("geras-paused-map-name"));
        _map.SetPaused(mapUid, true);
        PausedMap = mapUid;
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // create the geras entity and store it
        if (component.Entity != null)
        {

            EntityUid geras = Spawn(component.Entity, _transform.GetMapCoordinates(uid, Transform(uid)), rotation: _transform.GetWorldRotation(uid));
            component.Geras = geras;

            BanishEntity((geras, Transform(geras)));

            if (TryComp(uid, out MetaDataComponent? targetMeta))
                _metaData.SetEntityName(geras, targetMeta.EntityName);
            if (TryComp<HumanoidProfileComponent>(uid, out var profile))
                _bodySystem.ApplyProfile(geras, new() { SkinColor = profile.SkinColor });

            //tie the created entity's geras component to the original for reverse transformation
            var metaGeras = EnsureComp<GerasComponent>(geras);
            metaGeras.Geras = uid;

            //no need to load name/color for the initial player
            metaGeras.VisualsLoaded = true;
        }

        // try to add geras action
        _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return; // i hate zomber.

        if (!component.Geras.HasValue)
            return;



        var geras = component.Geras.Value;

        // Workaround to not knowing when a character's visuals are properly loaded, this definitely happens after that
        if(!component.VisualsLoaded)
            RaiseLocalEvent(uid, new GerasVisualInitEvent());

        // Drop all inventory items
        if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                _inventory.TryUnequip(uid, slot.ID, true, true);
            }
        }
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            _hands.TryDrop(uid, held);
        }

        var playerTransform = Transform(uid);
        var gerasTransform = Transform(geras);

        if (TerminatingOrDeleted(playerTransform.ParentUid))
            return;

        // Swap positions of initial entity and geras
        EnsurePausedMap();
        if (PausedMap != null)
        {
            _transform.SetParent(geras, gerasTransform, playerTransform.ParentUid);
            _transform.SetCoordinates(geras, gerasTransform, playerTransform.Coordinates, playerTransform.LocalRotation);

            _transform.SetParent(uid, playerTransform, PausedMap.Value);
        }

        // Apply damage to the new form
        if (TryComp<DamageableComponent>(geras, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, geras, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage((geras, damageParent), damage);
            _damageable.ClearAllDamage(uid);
        }



        // Transfer storage to transformed entity
        if (HasComp<StorageComponent>(geras) && TryComp<StorageComponent>(uid, out var parentStorage))
        {
            foreach (var item in parentStorage.StoredItems.Keys.ToList())
            {
                _storage.InsertAt(geras, item, parentStorage.StoredItems[item], out _, uid, playSound: false);
            }
        }

        _implantSystem.TransferImplants(uid, geras);

        // Transfer mind
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, geras, mind: mind);


        _popupSystem.PopupPredicted(
            Loc.GetString("geras-popup-morph-message-user"),
            Loc.GetString("geras-popup-morph-message-others", ("entity", geras)),
            geras,
            geras
        );

        args.Handled = true;
    }

    /// <summary>
    /// Sends an entity to the void for storage
    /// </summary>
    /// <param name="uid">the entity to be banished</param>
    private void BanishEntity(Entity<TransformComponent> uid)
    {
        EnsurePausedMap();
        if(PausedMap != null)
            _transform.SetParent(uid, uid.Comp, PausedMap.Value);
    }

    private void OnGerasVisualInit(Entity<GerasComponent> uid, ref GerasVisualInitEvent args)
    {
        if (!uid.Comp.Geras.HasValue)
            return;

        var geras = uid.Comp.Geras.Value;
        if (TryComp(uid, out MetaDataComponent? targetMeta))
            _metaData.SetEntityName(geras, targetMeta.EntityName);
        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
            _bodySystem.ApplyProfile(geras, new() { SkinColor = profile.SkinColor });
        uid.Comp.VisualsLoaded = true;
    }

    private void OnRemoveGeras(EntityUid uid, GerasComponent component, ComponentShutdown args)
    {
        QueueDel(component.Geras);
    }
}

public record struct GerasVisualInitEvent(Entity<GerasComponent> Uid);
