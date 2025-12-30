using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Objectives;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedPotentialObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PotentialObjectivesComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<PotentialObjectivesComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.AutoSelectionTime = _timing.CurTime + ent.Comp.AutoSelectionDelay;
        Dirty(ent);
    }
}
