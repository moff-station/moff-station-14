using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Objectives;

/// <summary>
/// This handles...
/// </summary>
public sealed class PotentialObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PotentialObjectivesComponent, MapInitEvent>(OnInit);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PotentialObjectivesComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.AutoSelectionTime)
                return;

            var objectives = comp.ObjectiveOptions.OrderBy(x => _random.Next())
                .Take(comp.MaxOptions)
                .ToDictionary()
                .Keys.ToHashSet();

            var ev = new ObjectivePickerSelected()
            {
                MindId = GetNetEntity(uid),
                SelectedObjectives = objectives,
            };
            RaiseNetworkEvent(ev);
            RemComp<PotentialObjectivesComponent>(uid);
        }
    }

    private void OnInit(Entity<PotentialObjectivesComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.AutoSelectionTime = _timing.CurTime + ent.Comp.AutoSelectionDelay;
    }
}
