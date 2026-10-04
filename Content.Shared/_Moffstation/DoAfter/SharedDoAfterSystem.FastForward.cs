// NOT a moffstation namespace because this partial class appends behavior to an existing class.
namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem
{
    /// <summary>
    /// Completes a running DoAfter now, as if its full delay had elapsed.
    /// </summary>
    public bool TryFastForward(Entity<DoAfterComponent?> ent, ushort id)
    {
        if (!Resolve(ent, ref ent.Comp, false) ||
            !ent.Comp.DoAfters.TryGetValue(id, out var doAfter) ||
            doAfter.Cancelled ||
            doAfter.Completed)
            return false;

        doAfter.StartTime = GameTiming.CurTime - doAfter.Args.Delay;
        TryComplete(doAfter, ent.Comp);
        Dirty(ent.Owner, ent.Comp);
        return !doAfter.Cancelled;
    }
}
