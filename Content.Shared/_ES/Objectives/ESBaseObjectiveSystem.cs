using System.Linq;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Objectives;

/// <summary>
///     This is a base class for objectives that need certain common behavior like relays.
///     This ensures they're always implemented correctly, instead of copy-pasting the mind added/removed logic for them.
/// </summary>
public abstract class ESBaseObjectiveSystem<TComponent> : EntitySystem
    where TComponent: Component
{
    [Dependency] protected readonly SharedMindSystem MindSys = default!;
    [Dependency] protected readonly ESSharedObjectiveSystem ObjectivesSys = default!;

    /// <summary>
    ///     A list of all the relays this objective relies on existing.
    /// </summary>
    /// <remarks>
    ///     This should not be used for things that modify behavior!
    ///     Relays are transient and automatically applied as the mind moves between bodies.
    ///     Relays <b>should be</b> stateless.
    /// </remarks>
    public virtual Type[] RelayComponents => Array.Empty<Type>();

    /// <inheritdoc/>
    public override void Initialize()
    {
        DebugTools.Assert(
            RelayComponents.All(x => x.IsAssignableTo(typeof(IComponent))),
            $"One or more relay components on {GetType()} aren't actual components. Check for typos."
            );

        SubscribeLocalEvent<TComponent, MindGotRemovedEvent>(OnMindGotRemoved);
        SubscribeLocalEvent<TComponent, MindGotAddedEvent>(OnMindGotAdded);
        SubscribeLocalEvent<TComponent, ESObjectiveAddedEvent>(OnObjectiveAfterAssign);

        SubscribeLocalEvent<TComponent, ESGetObjectiveProgressEvent>(GetObjectiveProgress);
        SubscribeLocalEvent<TComponent, ESInitializeObjectiveEvent>(InitializeObjective);
    }

    /// <summary>
    ///     Called after an objective is assigned, should call base before your own logic as it implements some
    ///     necessary logic for managing relays/etc.
    /// </summary>
    [MustCallBase]
    protected virtual void OnObjectiveAfterAssign(Entity<TComponent> ent, ref ESObjectiveAddedEvent args)
    {
        EnsureRelaysOnMind(args.Holder);
    }

    /// <summary>
    ///     Resolves progress on the objective.
    /// </summary>
    /// <remarks>
    ///     This is just an event subscription, made mandatory so you don't forget.
    /// </remarks>
    protected virtual void GetObjectiveProgress(Entity<TComponent> ent, ref ESGetObjectiveProgressEvent args)
    {

    }

    protected virtual void InitializeObjective(Entity<TComponent> ent, ref ESInitializeObjectiveEvent args)
    {

    }

    /// <summary>
    ///     Implements some necessary logic for managing relays/etc, should call base before your own logic.
    /// </summary>
    [MustCallBase]
    protected virtual void OnMindGotAdded(Entity<TComponent> ent, ref MindGotAddedEvent args)
    {
        EnsureRelaysOnMind(args.Mind.AsNullable());
    }

    private void EnsureRelaysOnMind(Entity<MindComponent?> mind)
    {
        // Not everything holding objectives is a mind.
        if (!Resolve(mind, ref mind.Comp, false))
            return;

        if (mind.Comp.CurrentEntity is {} body)
        {
            foreach (var relayType in RelayComponents)
            {
                if (!HasComp(body, relayType))
                    AddComp(body, Factory.GetComponent(relayType));
            }
        }
    }

    /// <summary>
    ///     Implements some necessary logic for managing relays/etc, should call base before your own logic.
    /// </summary>
    [MustCallBase]
    protected virtual void OnMindGotRemoved(Entity<TComponent> ent, ref MindGotRemovedEvent args)
    {
        // TODO(Kaylie): We don't actually do anything here.
        // Maybe in the future we can refcount the relays and remove them when no objectives are left that care
        // about that relay, but that's not terribly important. We have this override for orthogonality so if we do
        // change that in the future we don't need a big refactor.
    }
}
