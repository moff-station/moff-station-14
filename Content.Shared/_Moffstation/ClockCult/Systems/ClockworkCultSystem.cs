using Content.Shared.Actions;
using Content.Shared._Moffstation.ClockCult.Components;
using Content.Shared._Moffstation.ClockCult.Events;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
// Fuck it, we copy the entire "using" list.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;
//For some reason, this can't access things in Content other than the stuff in Shared. Yes, I don't like it.
public sealed class ClockworkCultSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkCultComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ClockworkCultComponent, ClockworkAbscondActionEvent>(OnAbscondAction);
        SubscribeLocalEvent<ClockworkCultComponent, ClockworkAbscondDoAfterEvent>(OnAbscondWindup);
        SubscribeLocalEvent<ClockworkCultComponent, ClockworkAbscondPullingDoAfterEvent>(OnAbscondPullWindup);
        SubscribeLocalEvent<ClockworkCultComponent, ComponentShutdown>(OnShutdown);
    }

    // Adds and Removes the action with the component
    private void OnMapInit(Entity<ClockworkCultComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.ClockworkAbscondActionEntity, ent.Comp.ClockworkAbscondAction);
    }
    private void OnShutdown(Entity<ClockworkCultComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ClockworkAbscondActionEntity != null)
        {
            _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ClockworkAbscondActionEntity);
        }
    }

    // Janky-ass Abscond code
    private void OnAbscondAction(Entity<ClockworkCultComponent> ent, ref ClockworkAbscondActionEvent args)
    {
        if (!TryComp<PullerComponent>(ent.Owner, out var pullComp))
            return;

        if (pullComp.Pulling != null) // Are they pulling someone?
        {
            EntityUid schmuck = pullComp.Pulling.Value;
            if (TryComp<TransformComponent>(schmuck, out var pullLocation))
            {

                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
                    EntityManager,
                    ent,
                    ent.Comp.AbscondWindup,
                    new ClockworkAbscondPullingDoAfterEvent(),
                    ent)
                {
                    BreakOnMove = true,
                    NeedHand = true, //Lizards are immune to this, I'm too tired to find a fix for this.
                    BreakOnHandChange = true,
                    BlockDuplicate = true,
                    DuplicateCondition = DuplicateConditions.None,
                });

            }
        }
        else
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
                EntityManager,
                ent,
                ent.Comp.AbscondWindup,
                new ClockworkAbscondDoAfterEvent(),
                ent)
            {
                BreakOnMove = true,
                BlockDuplicate = true,
                DuplicateCondition = DuplicateConditions.None,
            });
        }

    }

    // Runs after the windup if the cultist was pulling someone
    private void OnAbscondPullWindup(Entity<ClockworkCultComponent> ent, ref ClockworkAbscondPullingDoAfterEvent args)
    {
        //Immediately ends if unfinished.
        if (args.Cancelled)
            return;
        var position = new Vector2(0, 5); //I'll code this in to be a variable later...
        if (!TryComp<PullerComponent>(ent.Owner, out var pullComp))
            return;

        EntityUid? mapID = null;
        foreach (var checkMapId in _mapSystem.GetAllMapIds()) // Gets Reebe's MapId in the MapId? variable.
        {
            if (!_mapSystem.TryGetMap(checkMapId, out var mapUid))
                continue;
            if (_entManager.GetComponent<MetaDataComponent>(mapUid.Value).EntityName == "City of Cogs" && mapUid != null)
            {
                mapID = mapUid;
            }

        }
        if (mapID == null)
        {
            return; //Reebe doesn't exist, PANIC!!!
        }
        EntityUid safeMapId = mapID.Value;

        if (pullComp.Pulling != null) // Teleports ONLY if they're still pulling
        {
            if (TryComp<TransformComponent>(pullComp.Pulling.Value, out var pullTransform) && TryComp<TransformComponent>(ent.Owner, out var transfrom))
            {
                //Move target to Reebe
                _transform.SetWorldPosition((pullComp.Pulling.Value, pullTransform), position);
                _transform.SetParent(pullComp.Pulling.Value, pullTransform, safeMapId);
                //Move user to Reebe
                _transform.SetWorldPosition((ent.Owner, transfrom), position);
                _transform.SetParent(ent.Owner, transfrom, safeMapId);
            }
        }
    }


    // Runs after the windup if the cultist was pulling someone
    private void OnAbscondWindup(Entity<ClockworkCultComponent> ent, ref ClockworkAbscondDoAfterEvent args)
    {
        //Immediately ends if unfinished.
        if (args.Cancelled)
            return;
        var position = new Vector2(0, 5); //I'll code this in to be a variable later...

        EntityUid? mapID = null;
        foreach (var checkMapId in _mapSystem.GetAllMapIds()) // Gets Reebe's MapId in the MapId? variable.
        {
            if (!_mapSystem.TryGetMap(checkMapId, out var mapUid))
                continue;
            if (_entManager.GetComponent<MetaDataComponent>(mapUid.Value).EntityName == "City of Cogs" && mapUid != null)
            {
                mapID = mapUid;
            }

        }
        if (mapID == null)
        {
            return; //Reebe doesn't exist, what did you DO
        }
        EntityUid safeMapId = mapID.Value;

        if (TryComp<TransformComponent>(ent.Owner, out var transfrom))
        {
            //Move user to Reebe
            _transform.SetWorldPosition((ent.Owner, transfrom), position);
            _transform.SetParent(ent.Owner, transfrom, safeMapId);
        }
    }


}