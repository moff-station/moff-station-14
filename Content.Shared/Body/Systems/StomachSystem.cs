using Content.Shared._Moffstation.Body.Events;// Moffstation - Geras Patch
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Body.Systems;

public sealed partial class StomachSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    [Dependency] private BodySystem _body = default!;// Moffstation - Geras Patch

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
            TryTransferSolution(new Entity<StomachComponent?, SolutionManagerComponent?>(ent, ent, null), sourceSolution);
    }

    private void OnEmptyStomach(Entity<StomachComponent> ent, ref BodyRelayedEvent<EmptyStomachEvent> args)
    {
        if (ent.Comp.Solution is { } solution)
            _solutionContainerSystem.RemoveAllSolution(solution);
    }
    // Moffstation - End

    public bool CanTransferSolution(Entity<StomachComponent?, SolutionManagerComponent?> entity, Solution solution)
    {
        return Resolve(entity, ref entity.Comp1, logMissing: false)
            && _solutionContainerSystem.ResolveSolution((entity, entity.Comp2), DefaultSolutionName, ref entity.Comp1.Solution, out var stomachSolution)
            // TODO: For now no partial transfers. Potentially change by design
            && stomachSolution.CanAddSolution(solution);
    }

    public bool TryTransferSolution(Entity<StomachComponent?, SolutionManagerComponent?> entity, Solution solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false)
            || !_solutionContainerSystem.ResolveSolution((entity, entity.Comp2), DefaultSolutionName, ref entity.Comp1.Solution)
            || !CanTransferSolution(entity, solution))
            return false;

        _solutionContainerSystem.TryAddSolution(entity.Comp1.Solution.Value, solution);
        return true;
    }
}
