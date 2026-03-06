using System.Linq; // Moffstation
using Content.Server.Administration.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.EUI;
using Content.Shared._Moffstation.Extensions; // Moffstation
using Content.Shared.Administration;
using Content.Shared.Body; // Moffstation
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems; // Moffstation
using Content.Shared.Eui;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers; // Moffstation
using Robust.Shared.Timing;

namespace Content.Server.Administration.UI
{
    /// <summary>
    ///     Admin Eui for displaying and editing the reagents in a solution.
    /// </summary>
    [UsedImplicitly]
    public sealed class EditSolutionsEui : BaseEui
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly SharedContainerSystem _container = default!; // Moffstation - Show organ solutions in solution editor
        private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        public readonly EntityUid Target;

        public EditSolutionsEui(EntityUid entity)
        {
            IoCManager.InjectDependencies(this);
            _solutionContainerSystem = _entityManager.System<SharedSolutionContainerSystem>();
            _container = _entityManager.System<SharedContainerSystem>(); // Moffstation - Show organ solutions in solution editor
            Target = entity;
        }

        public override void Opened()
        {
            base.Opened();
            StateDirty();
        }

        public override void Closed()
        {
            base.Closed();
            _entityManager.System<AdminVerbSystem>().OnEditSolutionsEuiClosed(Player, this);
        }

        public override EuiStateBase GetNewState()
        {
            // Moffstation - Begin - Show organ solutions in solution editor
            IEnumerable<EntityUid> solutionContainers = [Target];
            if (_container.TryGetContainer(Target, BodyComponent.ContainerID, out var bodyParts))
            {
                solutionContainers = solutionContainers.Concat(bodyParts.ContainedEntities);
            }

            var netSolutions = solutionContainers
                .SelectMany(it => _solutionContainerSystem.EnumerateSolutions(it))
                .SelectNotNull((string, NetEntity)? (it) =>
                {
                    if (it.Name is { } name && _entityManager.TryGetNetEntity(it.Solution, out var netSolution))
                        return (name, netSolution.Value);
                    return null;
                })
                .ToList();

            return new EditSolutionsEuiState(_entityManager.GetNetEntity(Target),
                netSolutions.Count != 0 ? netSolutions : null,
                _gameTiming.CurTick);
            // Moffstation - End
        }
    }
}
