using Content.Shared.Cloning.Events;
using Content.Shared._Moffstation.Traits.Assorted;

namespace Content.Server._Moffstation.Traits.Assorted;

public sealed class UncloneableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UncloneableComponent, CloningAttemptEvent>(OnCloningAttempt);
    }

    private void OnCloningAttempt(Entity<UncloneableComponent> ent, ref CloningAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
