using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Silicons.StationAi;

public sealed partial class AiShellSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly SharedSiliconLawSystem _siliconLaws = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiShellControllerComponent, ToggleAiShellControllerUiEvent>(OnToggleAiShellControllerUi);
        SubscribeLocalEvent<ControlledAiShellComponent, StopAiShellControlEvent>(OnStopAiShellControl);
        SubscribeLocalEvent<AiShellHolderComponent, StopAiShellControlEvent>(RelayToHeldShell);
        SubscribeLocalEvent<AiShellControllerComponent, GetVerbsEvent<InnateVerb>>(OnGetInnateVerbs);
        SubscribeLocalEvent<AiShellComponent, EntGotInsertedIntoContainerMessage>(OnShellGotInsertedEvent);
        SubscribeLocalEvent<AiShellComponent, EntGotRemovedFromContainerMessage>(OnShellGotRemovedEvent);

        SubscribeLocalEvent<AiShellComponent, ComponentInit>(OnAiShellInit);
        SubscribeLocalEvent<AiShellComponent, ComponentRemove>(OnAiShellRemoved);
        SubscribeLocalEvent<AiShellControllerComponent, ComponentInit>(OnAiShellControllerInit);
        SubscribeLocalEvent<AiShellControllerComponent, ComponentRemove>(OnAiShellControllerRemoved);
        SubscribeLocalEvent<ControlledAiShellComponent, ComponentInit>(OnControlledAiShellInit);
        SubscribeLocalEvent<ControlledAiShellComponent, ComponentRemove>(OnControlledAiShellRemoved);

        SubscribeLocalEvent<AiShellComponent, AiShellStartedBeingControlledEvent>(OnAiShellStartedBeingControlled);
        SubscribeLocalEvent<AiShellComponent, AiShellStoppedBeingControlledEvent>(OnAiShellStoppedBeingControlled);
        SubscribeLocalEvent<AiShellHolderComponent, ContainedAiShellStartedBeingControlledEvent>(
            OnContainedAiShellStartedBeingControlled);
        SubscribeLocalEvent<AiShellHolderComponent, ContainedAiShellStoppedBeingControlledEvent>(
            OnContainedAiShellStoppedBeingControlled);

        Subs.BuiEvents<AiShellControllerComponent>(ShellUiKey.Key,
            subs =>
            {
                subs.Event<SelectAiShellMessage>(OnSelectShell);
                subs.Event<JumpToAiShellMessage>(OnJumpToShell);
                subs.Event<StartAiShellControlMessage>(OnEnterShell);
            });
    }


    /// Gets the entity with <see cref="AiShellControllerComponent"/> which is currently controlling the given entity.
    /// Returns null if the given entity is not currently being controlled.
    [PublicAPI]
    public Entity<AiShellControllerComponent>? GetController(Entity<AiShellComponent> entity)
    {
        if (!TryComp<ControlledAiShellComponent>(entity, out var comp))
            return null;

        var controllerEnt = GetEntity(comp.Controller);
        if (!TryComp<AiShellControllerComponent>(controllerEnt, out var controller))
        {
            return this.AssertOrLogError<Entity<AiShellControllerComponent>?>(
                $"{ToPrettyString(entity)}'s controller entity does not have {nameof(AiShellControllerComponent)}",
                null
            );
        }

        return (controllerEnt, controller);
    }

    /// Returns the <see cref="BorgChassisComponent"/> which contains the given shell, if it is in the borg's brain slot.
    [PublicAPI]
    public Entity<BorgChassisComponent>? GetHolder(Entity<AiShellComponent> entity)
    {
        if (_container.GetContainingContainers(entity.Owner).FirstOrDefault() is not { } container ||
            !TryComp<BorgChassisComponent>(container.Owner, out var chassisComp) ||
            chassisComp.BrainEntity != entity)
            return null;

        return (container.Owner, chassisComp);
    }

    [PublicAPI]
    public void StartAiShellControl(Entity<AiShellControllerComponent> controller, Entity<AiShellComponent> shell)
    {
        if (controller.Comp.ControllingShell is { } existingShell)
        {
            this.AssertOrLogError(
                $"Terminating existing AI shell control of {ToPrettyString(existingShell)} by " +
                $"{ToPrettyString(controller)} in order to establish control of {ToPrettyString(shell)}"
            );
            StopAiShellControl(controller);
        }

        if (GetController(shell) is { } existingController)
        {
            this.AssertOrLogError(
                $"Failed to start AI shell control of {ToPrettyString(shell)} by " +
                $"{ToPrettyString(controller)} as the former is already under the control of" +
                $"{ToPrettyString(existingController)}"
            );
            return;
        }

        if (!_mind.TryGetMind(controller, out var mindId, out var mind))
        {
            this.AssertOrLogError(
                $"Tried to start AI shell control of {ToPrettyString(shell)} by " +
                $"{ToPrettyString(controller)}, but the latter does not have a mind"
            );
            return;
        }

        var holder = GetHolder(shell);
        _mind.TransferTo(mindId, holder?.Owner ?? shell.Owner, mind: mind);
        controller.Comp.ControllingShell = GetNetEntity(shell);
        Dirty(controller);

        var controlled = AddComp<ControlledAiShellComponent>(shell);
        controlled.Controller = GetNetEntity(controller);
        Dirty(shell, controlled);

        var controllerEvent = new AiShellControlStartedEvent(controller, shell, holder);
        RaiseLocalEvent(controller, ref controllerEvent);
        var controlledEvent = new AiShellStartedBeingControlledEvent(controller, shell, holder);
        RaiseLocalEvent(shell, ref controlledEvent);
        if (holder is { } h)
        {
            var holderEvent = new ContainedAiShellStartedBeingControlledEvent(controller, shell, h);
            RaiseLocalEvent(h, ref holderEvent);
        }
    }

    /// Ends <paramref name="controller"/>'s remote control session over its
    /// <see cref="AiShellControllerComponent.ControllingShell">currently controlled shell</see>. If the given entity
    /// does not currently remotely control a shell, this function does nothing.
    [PublicAPI]
    public void StopAiShellControl(Entity<AiShellControllerComponent> controller)
    {
        if (GetEntity(controller.Comp.ControllingShell) is not { } controllingShell ||
            !TryComp<AiShellComponent>(controllingShell, out var shellComp))
            // No shell to stop controlling.
            return;
        var shell = new Entity<AiShellComponent>(controllingShell, shellComp);

        if (!HasComp<ControlledAiShellComponent>(controllingShell))
        {
            this.AssertOrLogError(
                $"Failed to stop AI shell control of {ToPrettyString(controller)} over " +
                $"{ToPrettyString(controllingShell)}, as the latter does not have {nameof(ControlledAiShellComponent)}");
            return;
        }

        var holder = GetHolder(shell);

        var mind = new Entity<MindComponent?>();
        if (holder is { } h)
        {
            if (!_mind.TryGetMind(h, out mind.Owner, out mind.Comp))
            {
                this.AssertOrLogError(
                    $"{ToPrettyString(shell)} is in holder {ToPrettyString(h)}, but its " +
                    $"holder does not have a mind. Falling back to using the mind from the shell itself."
                );
                _mind.TryGetMind(shell, out mind.Owner, out mind.Comp);
            }

            _siliconLaws.UnlinkFromProvider(h.Owner);
        }
        else
        {
            _mind.TryGetMind(shell, out mind.Owner, out mind.Comp);
        }

        if (!mind.Owner.Valid || mind.Comp is null)
        {
            this.AssertOrLogError(
                $"Tried to stop AI shell control over {ToPrettyString(shell)}, " +
                $"but it does not have a mind."
            );
            return;
        }

        _mind.TransferTo(mind, controller, mind: mind.Comp);
        controller.Comp.ControllingShell = null;
        Dirty(controller);
        RemCompDeferred<ControlledAiShellComponent>(shell);
        Dirty(shell);

        var controllerEvent = new AiShellControlStoppedEvent(controller, shell, holder);
        RaiseLocalEvent(controller, ref controllerEvent);
        var controlledEvent = new AiShellStoppedBeingControlledEvent(controller, shell, holder);
        RaiseLocalEvent(shell, ref controlledEvent);
        if (holder is { } holderTarget)
        {
            var holderEvent = new ContainedAiShellStoppedBeingControlledEvent(controller, shell, holderTarget);
            RaiseLocalEvent(holderTarget, ref holderEvent);
        }
    }

    /// Ends remote control over <paramref name="shell"/>.
    /// See <see cref="StopAiShellControl(Entity{AiShellControllerComponent})"/> for details.
    [PublicAPI]
    public void StopAiShellControl(Entity<AiShellComponent> shell)
    {
        if (GetController(shell) is { } controller)
            StopAiShellControl(controller);
    }

    /// Ends remote control over <paramref name="shell"/>.
    /// See <see cref="StopAiShellControl(Entity{AiShellControllerComponent})"/> for details.
    [PublicAPI]
    public void StopAiShellControl(Entity<ControlledAiShellComponent> shell)
    {
        var entityUid = GetEntity(shell.Comp.Controller);
        if (TryComp<AiShellControllerComponent>(entityUid, out var comp))
            StopAiShellControl((entityUid, comp));
    }


    private void OnToggleAiShellControllerUi(
        Entity<AiShellControllerComponent> entity,
        ref ToggleAiShellControllerUiEvent args
    )
    {
        _ui.TryToggleUi(args.Performer, ShellUiKey.Key, entity);
    }

    private void OnStopAiShellControl(Entity<ControlledAiShellComponent> entity, ref StopAiShellControlEvent args)
    {
        StopAiShellControl(entity);
    }

    // Adds a "link" verb which lets you add new shells to your list. Without this, AIs would lose access to all
    // currently existing shells when carded.
    private void OnGetInnateVerbs(
        Entity<AiShellControllerComponent> entity,
        ref GetVerbsEvent<InnateVerb> args
    )
    {
        if (!args.CanComplexInteract || !args.CanAccess)
            return;

        Entity<AiShellComponent> shell;
        if (TryComp<AiShellHolderComponent>(args.Target, out var holderComp))
        {
            if (GetContainedShell((args.Target, holderComp)) is not { } shellInHolder)
            {
                this.AssertOrLogError(
                    $"{ToPrettyString(args.Target)} has {nameof(AiShellHolderComponent)}, " +
                    $"but does not contain a shell, this shouldn't be possible."
                );
                return;
            }

            shell = shellInHolder;
        }
        else if (TryComp<AiShellComponent>(args.Target, out var shellComp))
        {
            shell = (args.Target, shellComp);
        }
        else
        {
            // Neither a shell nor a shell holder.
            return;
        }

        if (entity.Comp.ControllableShells.Contains(shell))
            return;

        args.Verbs.Add(new InnateVerb
        {
            Text = Loc.GetString("ai-shell-verb-text-link"),
            Act = () =>
            {
                AddToAvailableShells(entity, shell);
            },
            IconEntity = GetNetEntity(args.Target),
        });
    }

    private void OnShellGotInsertedEvent(Entity<AiShellComponent> shell, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!TryComp<BorgChassisComponent>(args.Container.Owner, out var chassisComp) ||
            args.Container.ID != chassisComp.BrainContainerId)
            return;
        var holder = new Entity<BorgChassisComponent>(args.Container.Owner, chassisComp);
        EnsureComp<AiShellHolderComponent>(holder);

        // Transfer ongoing session, if it exists.
        if (GetController(shell) is { } controller)
        {
            if (!_mind.TryGetMind(shell, out var mind, out var mindComp))
            {
                this.AssertOrLogError(
                    $"{ToPrettyString(shell)} has controller {ToPrettyString(controller)} " +
                    $"but no mind.");
                return;
            }

            _mind.TransferTo(mind, holder, mind: mindComp);

            var ev = new ContainedAiShellStartedBeingControlledEvent(controller, shell, holder);
            RaiseLocalEvent(holder, ref ev);
        }
    }

    private void OnShellGotRemovedEvent(Entity<AiShellComponent> shell, ref EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<BorgChassisComponent>(args.Container.Owner, out var chassisComp) ||
            args.Container.ID != chassisComp.BrainContainerId)
            return;
        var holder = new Entity<BorgChassisComponent>(args.Container.Owner, chassisComp);
        RemCompDeferred<AiShellHolderComponent>(holder);

        // Transfer ongoing session, if it exists.
        if (GetController(shell) is { } controller)
        {
            if (!_mind.TryGetMind(holder, out var mind, out var mindComp))
            {
                this.AssertOrLogError(
                    $"{ToPrettyString(shell)} removed from holder {ToPrettyString(holder)} has " +
                    $"controller {ToPrettyString(controller)}, but the holder has no mind.");
                return;
            }

            _mind.TransferTo(mind, shell, mind: mindComp);

            var ev = new ContainedAiShellStoppedBeingControlledEvent(controller, shell, holder);
            RaiseLocalEvent(holder, ref ev);
        }
    }

    private void OnAiShellInit(Entity<AiShellComponent> entity, ref ComponentInit args)
    {
        var query = EntityQueryEnumerator<AiShellControllerComponent>();
        while (query.MoveNext(out var controllerEnt, out var controllerComp))
        {
            AddToAvailableShells((controllerEnt, controllerComp), entity);
        }
    }

    private void OnAiShellRemoved(Entity<AiShellComponent> entity, ref ComponentRemove args)
    {
        var query = EntityQueryEnumerator<AiShellControllerComponent>();
        while (query.MoveNext(out var controllerEnt, out var controllerComp))
        {
            RemoveFromAvailableShells((controllerEnt, controllerComp), entity);
        }
    }

    private void OnAiShellControllerInit(Entity<AiShellControllerComponent> entity, ref ComponentInit args)
    {
        _actions.AddAction(
            entity,
            ref entity.Comp.ToggleUiAction,
            AiShellControllerComponent.ToggleUiActionProto
        );
    }

    private void OnAiShellControllerRemoved(Entity<AiShellControllerComponent> entity, ref ComponentRemove args)
    {
        _actions.RemoveAction(entity.Owner, entity.Comp.ToggleUiAction);
    }

    private void OnControlledAiShellInit(Entity<ControlledAiShellComponent> entity, ref ComponentInit args)
    {
        _actions.AddAction(
            entity,
            ref entity.Comp.StopControlAction,
            ControlledAiShellComponent.StopControlActionProto
        );
    }

    private void OnControlledAiShellRemoved(Entity<ControlledAiShellComponent> entity, ref ComponentRemove args)
    {
        _actions.RemoveAction(entity.Owner, entity.Comp.StopControlAction);
    }

    private void OnAiShellStartedBeingControlled(
        Entity<AiShellComponent> entity,
        ref AiShellStartedBeingControlledEvent args
    )
    {
        _siliconLaws.LinkToProvider(entity.Owner, args.Controller.Owner);
    }

    private void OnAiShellStoppedBeingControlled(
        Entity<AiShellComponent> entity,
        ref AiShellStoppedBeingControlledEvent args
    )
    {
        _siliconLaws.UnlinkFromProvider(entity.Owner);
    }

    private void OnContainedAiShellStartedBeingControlled(
        Entity<AiShellHolderComponent> entity,
        ref ContainedAiShellStartedBeingControlledEvent args
    )
    {
        _siliconLaws.LinkToProvider(entity.Owner, args.Controller.Owner);
        _actions.AddAction(
            entity,
            ref entity.Comp.StopControlAction,
            ControlledAiShellComponent.StopControlActionProto
        );
        _metaData.SetEntityName(entity, Name(args.Controller));
    }

    private void OnContainedAiShellStoppedBeingControlled(
        Entity<AiShellHolderComponent> entity,
        ref ContainedAiShellStoppedBeingControlledEvent args
    )
    {
        _siliconLaws.UnlinkFromProvider(entity.Owner);
        _actions.RemoveAction(entity.Owner, entity.Comp.StopControlAction);
        _metaData.SetEntityName(entity, Loc.GetString(AiShellHolderComponent.EmptyHolderName));
    }


    private void OnSelectShell(Entity<AiShellControllerComponent> controller, ref SelectAiShellMessage args)
    {
        Entity<AiShellComponent>? shell = null;
        if (args.Shell is { } netShell)
        {
            var entity = GetEntity(netShell);
            if (!TryComp<AiShellComponent>(entity, out var shellComp))
            {
                this.AssertOrLogError(
                    $"Received {nameof(SelectAiShellMessage)} with shell={ToPrettyString(entity)} without {nameof(AiShellComponent)}");
                return;
            }

            shell = (entity, shellComp);
        }

        controller.Comp.SelectedShell = shell;
        Dirty(controller);
        UpdateUi(controller);
    }

    private void OnJumpToShell(Entity<AiShellControllerComponent> controller, ref JumpToAiShellMessage args)
    {
        if (!_stationAiSystem.TryGetCore(controller, out var coreEnt) ||
            coreEnt.Comp is not { RemoteEntity: { } aiEye })
            return;

        _xforms.DropNextTo(aiEye, GetEntity(args.Shell));
    }


    private void OnEnterShell(Entity<AiShellControllerComponent> controller, ref StartAiShellControlMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var shell = GetEntity(args.Shell);
        if (!TryComp<AiShellComponent>(shell, out var shellComp))
        {
            this.AssertOrLogError(
                $"Received {nameof(SelectAiShellMessage)} with shell={ToPrettyString(shell)} without {nameof(AiShellComponent)}");
            return;
        }

        StartAiShellControl(controller, (shell, shellComp));
    }


    /// <summary>
    /// Add a shell to an AI's list of controllable shells
    ///  </summary>
    /// <param name="shellUser">The AI that is capable of controlling shells</param>
    /// <param name="shell">The shell to be made available to the controlling AI</param>
    private void AddToAvailableShells(Entity<AiShellControllerComponent> shellUser, Entity<AiShellComponent> shell)
    {
        shellUser.Comp.ControllableShells.Add(shell);
        Dirty(shellUser);
        UpdateUi(shellUser);
    }

    /// <summary>
    /// Remove a shell from an AI's list of controllable shells
    /// </summary>
    /// <param name="shellUser">The AI that is capable of controlling shells</param>
    /// <param name="shell">The shell to be made available to the controlling AI</param>
    private void RemoveFromAvailableShells(
        Entity<AiShellControllerComponent> shellUser,
        Entity<AiShellComponent> shell
    )
    {
        shellUser.Comp.ControllableShells.Remove(shell);
        Dirty(shellUser);
        UpdateUi(shellUser);
    }

    /// Gets the brain from the given entity. Returns null if the given entity does not have a
    /// <see cref="AiShellHolderComponent"/>, or in the exceptional case that the given entity does have
    /// that component, but does not have a <see cref="BorgChassisComponent"/> containin a
    /// <see cref="AiShellComponent"/>.
    private Entity<AiShellComponent>? GetContainedShell(Entity<AiShellHolderComponent?> entity)
    {
        if (!HasComp<AiShellHolderComponent>(entity) ||
            !TryComp<BorgChassisComponent>(entity, out var chassis) ||
            chassis.BrainEntity is not { } brainEnt ||
            !TryComp<AiShellComponent>(brainEnt, out var brain))
            return null;

        return (brainEnt, brain);
    }


    /// Causes the given entity's <see cref="ShellUiKey">Station AI Shell selection UI</see> to be updated with its
    /// current state.
    private void UpdateUi(Entity<AiShellControllerComponent> entity)
    {
        _ui.SetUiState(
            entity.Owner,
            ShellUiKey.Key,
            new AiShellControllerBuiState(
                GetNetEntity(entity.Comp.SelectedShell),
                GetNetEntityList(entity.Comp.ControllableShells)
            )
        );
    }

    private void RelayToHeldShell<T>(Entity<AiShellHolderComponent> holder, ref T args) where T : notnull
    {
        if (GetContainedShell(holder.AsNullable()) is not { } shell)
        {
            this.AssertOrLogError(
                $"Shell holder {ToPrettyString(holder)} does not contain a shell. This should be impossible.");
            return;
        }

        RaiseLocalEvent(shell, args);
    }
}
