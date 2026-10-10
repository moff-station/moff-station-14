namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem
{
    /// <summary>
    /// Completes a DoAfter immediately.
    /// </summary>
    public bool TryComplete(Entity<DoAfterComponent?> ent, ushort id)
    {
        if (!Resolve(ent, ref ent.Comp, false) ||
            !ent.Comp.DoAfters.TryGetValue(id, out var doAfter))
            return false;

        doAfter.StartTime = GameTiming.CurTime - doAfter.Args.Delay;
        TryComplete(doAfter, ent.Comp);
        Dirty(ent.Owner, ent.Comp);
        return !doAfter.Cancelled;
    }
}
