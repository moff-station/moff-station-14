using Content.Shared._Moffstation.Body.Events;// Moffstation - Geras Patch
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

public sealed class StomachSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly BodySystem _body = default!;// Moffstation - Geras Patch

    public const string DefaultSolutionName = "stomach";

    // Moffstation - Geras Patch - Begin
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, GetStomachContentsEvent>(BodyRelayEvent);
        SubscribeLocalEvent<BodyComponent, ApplyStomachContentsEvent>(BodyRelayEvent);
        SubscribeLocalEvent<BodyComponent, EmptyStomachEvent>(BodyRelayEvent);
        SubscribeLocalEvent<StomachComponent, BodyRelayedEvent<GetStomachContentsEvent>>(OnGetStomachContents);
        SubscribeLocalEvent<StomachComponent, BodyRelayedEvent<ApplyStomachContentsEvent>>(OnApplyStomachContents);
        SubscribeLocalEvent<StomachComponent, BodyRelayedEvent<EmptyStomachEvent>>(OnEmptyStomach);
    }

    private void BodyRelayEvent<TEvent>(Entity<BodyComponent> ent, ref TEvent args) where TEvent : struct
    {
        _body.RelayEvent(ent, ref args);
    }

    private void OnGetStomachContents(Entity<StomachComponent> ent, ref BodyRelayedEvent<GetStomachContentsEvent> args)
    {
        if (args.Args.Handled)
            return;

        if (ent.Comp.Solution is { } solution)
        {
            args.Args = args.Args with { Contents = solution, Handled = true };
        }
    }

    private void OnApplyStomachContents(Entity<StomachComponent> ent, ref BodyRelayedEvent<ApplyStomachContentsEvent> args)
    {
        if (args.Args.Contents is not { } contents)
            return;

        if ( contents.Solution is { } sourceSolution)
            TryTransferSolution(ent, sourceSolution, ent.Comp);
    }

    private void OnEmptyStomach(Entity<StomachComponent> ent, ref BodyRelayedEvent<EmptyStomachEvent> args)
    {
        if (ent.Comp.Solution is { } solution)
            _solutionContainerSystem.RemoveAllSolution(solution);
    }
    // Moffstation - End

    public bool CanTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        return Resolve(uid, ref stomach, ref solutions, logMissing: false)
            && _solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution, out var stomachSolution)
            // TODO: For now no partial transfers. Potentially change by design
            && stomachSolution.CanAddSolution(solution);
    }

    public bool TryTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        if (!Resolve(uid, ref stomach, ref solutions, logMissing: false)
            || !_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution)
            || !CanTransferSolution(uid, solution, stomach, solutions))
        {
            return false;
        }

        _solutionContainerSystem.TryAddSolution(stomach.Solution.Value, solution);

        return true;
    }

    // Moffstation - Start - Helper functions for the StomachSystem
    public float MaxTransferableSolution(
        EntityUid uid,
        float quantity,
        Solution? solution = null,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        return Resolve(uid, ref stomach, ref solutions, logMissing: false)
               && _solutionContainerSystem.ResolveSolution((uid, solutions),
                   DefaultSolutionName,
                   ref stomach.Solution,
                   out var stomachSolution)
            ? stomachSolution.MaxTransferableSolution(quantity, solution)
            : 0.0f;
    }
    // Moffstation - End
}
