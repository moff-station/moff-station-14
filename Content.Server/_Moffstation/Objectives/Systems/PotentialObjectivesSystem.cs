using System.Linq;
using Content.Shared._Moffstation.Objectives;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Objectives.Systems;

public sealed partial class PotentialObjectivesSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PotentialObjectivesComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.AutoSelectionTime)
                return;

            var objectives = comp.ObjectiveOptions.OrderBy(_ => _random.Next())
                .Take(comp.MaxChoices)
                .Select(it => it.Key)
                .ToHashSet();

            var ev = new ObjectivePickerSelected
            {
                MindId = GetNetEntity(uid),
                SelectedObjectives = objectives,
            };
            RaiseLocalEvent(ev);
        }
    }
}
