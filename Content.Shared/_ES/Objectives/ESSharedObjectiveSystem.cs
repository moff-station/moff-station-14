using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Objectives;

/// <summary>
/// Handles assignment and core logic of objectives for ES.
/// </summary>
public abstract partial class ESSharedObjectiveSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeCounter();

        SubscribeLocalEvent<ESObjectiveHolderComponent, MindGotAddedEvent>(OnMindGotAdded);
        SubscribeLocalEvent<ESObjectiveHolderComponent, MindGotRemovedEvent>(OnMindGotRemoved);
    }

    private void OnMindGotAdded(Entity<ESObjectiveHolderComponent> ent, ref MindGotAddedEvent args)
    {
        foreach (var objective in GetObjectives(ent.AsNullable()))
        {
            RaiseLocalEvent(objective, args);
        }
    }

    private void OnMindGotRemoved(Entity<ESObjectiveHolderComponent> ent, ref MindGotRemovedEvent args)
    {
        foreach (var objective in GetObjectives(ent.AsNullable()))
        {
            RaiseLocalEvent(objective, args);
        }
    }

    /// <summary>
    /// Queries an objective to determine what its current progress is.
    /// </summary>
    public void RefreshObjectiveProgress(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new ESGetObjectiveProgressEvent();
        RaiseLocalEvent(ent, ref ev);

        var oldProgress = ent.Comp.Progress;
        var newProgress = Math.Clamp(ev.Progress, 0, 1);

        // If they are unchanged, then don't update anything.
        if (MathHelper.CloseTo(oldProgress, newProgress))
            return;

        ent.Comp.Progress = newProgress;

        var afterEv = new ESObjectiveProgressChangedEvent((ent, ent.Comp), oldProgress, newProgress);
        RaiseLocalEvent(ent, ref afterEv);

        Dirty(ent);
    }

    /// <summary>
    /// Gets the cached progress of an objective on [0, 1]
    /// If you need to update the progress, use <see cref="RefreshObjectiveProgress"/>
    /// </summary>
    public float GetProgress(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0;
        return ent.Comp.Progress;
    }

    /// <summary>
    /// Checks if a given objective is completed.
    /// </summary>
    public bool IsCompleted(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;
        return GetProgress(ent) >= 1 || MathHelper.CloseTo(GetProgress(ent), 1);
    }

    public void RefreshObjectives(Entity<ESObjectiveHolderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var oldObjectives = new List<EntityUid>(ent.Comp.Objectives);
        var newObjectives = new List<EntityUid>();

        var ev = new ESGetAdditionalObjectivesEvent((ent, ent.Comp), []);
        RaiseLocalEvent(ent, ref ev);

        newObjectives.AddRange(ev.Objectives.Select(e => e.Owner));
        newObjectives.AddRange(ent.Comp.OwnedObjectives);

        var added = newObjectives.Except(oldObjectives).ToList();
        var removed = oldObjectives.Except(newObjectives).ToList();

        // Exit early if nothing has changed
        if (added.Count == 0 && removed.Count == 0)
            return;

        ent.Comp.Objectives = newObjectives;
        Dirty(ent);

        // If this holder has a player occupying it, update network status of objectives.
        // TODO: maybe make this an ESObjectivesChangedEvent sub?
        if (TryComp<MindComponent>(ent, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
        {
            foreach (var obj in added)
            {
                _pvsOverride.AddSessionOverride(obj, session);

                var addedEv = new ESObjectiveAddedEvent(ent, obj);
                RaiseLocalEvent(obj, ref addedEv);
            }
            foreach (var obj in removed)
            {
                _pvsOverride.RemoveSessionOverride(obj, session);

                var removedEv = new ESObjectiveRemovedEvent(ent, obj);
                RaiseLocalEvent(obj, ref removedEv);
            }
        }

        var changedEv = new ESObjectivesChangedEvent(newObjectives, added, removed);
        RaiseLocalEvent(ent, ref changedEv);
    }

    /// <summary>
    /// Returns all objectives on an entity
    /// </summary>
    [PublicAPI]
    public List<Entity<ESObjectiveComponent>> GetObjectives(Entity<ESObjectiveHolderComponent?> ent)
    {
        return GetObjectives<ESObjectiveComponent>(ent);
    }

    /// <summary>
    /// Returns all objectives on an entity which have a given component
    /// </summary>
    public List<Entity<T>> GetObjectives<T>(Entity<ESObjectiveHolderComponent?> ent) where T : Component
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return [];

        var objectives = new List<Entity<T>>();

        foreach (var objective in ent.Comp.Objectives)
        {
            if (!TryComp<T>(objective, out var comp))
                continue;

            objectives.Add((objective, comp));
        }

        return objectives;
    }

    /// <summary>
    /// Returns all objectives which have a given component
    /// </summary>
    public List<Entity<T>> GetObjectives<T>() where T : Component
    {
        var query = EntityQueryEnumerator<T, ESObjectiveComponent>();

        var objectives = new List<Entity<T>>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            objectives.Add((uid, comp));
        }

        return objectives;
    }

    /// <summary>
    /// <inheritdoc cref="CanAddObjective(Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveComponent?},Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveHolderComponent?})"/>
    /// </summary>
    [PublicAPI]
    public bool CanAddObjective(EntProtoId protoId, Entity<ESObjectiveHolderComponent?> holder)
    {
        var objectiveUid = EntityManager.PredictedSpawn(protoId, MapCoordinates.Nullspace);
        var objectiveComp = Comp<ESObjectiveComponent>(objectiveUid);
        var objectiveEnt = (objectiveUid, objectiveComp);

        var val = CanAddObjective(objectiveEnt, holder);

        // always destroy objectives created in this method.
        Del(objectiveUid);
        return val;
    }

    /// <summary>
    /// Checks if a given objective can be added
    /// </summary>
    public bool CanAddObjective(Entity<ESObjectiveComponent> ent, Entity<ESObjectiveHolderComponent?> holder)
    {
        // STUB: add events
        // TODO: the reason this isn't blocked out is because EntityTable selection (what we use objectives)
        // doesn't have real remedial behavior when deciding what objectives to select. So, at least for right now,
        // an objective failing to assign doesnt really mean anything and it just kinda results in Nothing occuring.

        return true;
    }

    /// <summary>
    /// <inheritdoc cref="TryAddObjective(Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveHolderComponent?},Robust.Shared.Prototypes.EntProtoId,out Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveComponent}?,bool)"/>
    /// </summary>
    public bool TryAddObjective(Entity<ESObjectiveHolderComponent?> ent, EntProtoId protoId)
    {
        return TryAddObjective(ent, protoId, out _);
    }

    /// <summary>
    /// Attempts to create and add multiple objectives
    /// </summary>
    /// <returns>Returns true if all objectives succeed</returns>
    public bool TryAddObjective(Entity<ESObjectiveHolderComponent?> ent, EntityTableSelector table)
    {
        var spawns = _entityTable.GetSpawns(table);
        return spawns.All(e => TryAddObjective(ent, e));
    }

    /// <summary>
    /// Attempts to create and assign an objective to an entity
    /// </summary>
    /// <param name="ent">The entity that will be assigned the objective</param>
    /// <param name="protoId">Prototype for the objective</param>
    /// <param name="objective">The newly created objective entity</param>
    public bool TryAddObjective(
        Entity<ESObjectiveHolderComponent?> ent,
        EntProtoId protoId,
        [NotNullWhen(true)] out Entity<ESObjectiveComponent>? objective)
    {
        objective = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        var objectiveUid = EntityManager.PredictedSpawn(protoId, MapCoordinates.Nullspace);
        var objectiveComp = Comp<ESObjectiveComponent>(objectiveUid);
        objective = (objectiveUid, objectiveComp);

        if (!CanAddObjective(objective.Value, ent))
        {
            Del(objective);
            return false;
        }

        var ev = new ESInitializeObjectiveEvent((ent, ent.Comp));
        RaiseLocalEvent(objectiveUid, ref ev);

        ent.Comp.OwnedObjectives.Add(objective.Value);
        RefreshObjectives(ent);
        RefreshObjectiveProgress(objective.Value.AsNullable());
        return true;
    }
}
