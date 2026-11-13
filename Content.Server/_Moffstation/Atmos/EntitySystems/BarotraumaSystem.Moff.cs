using Content.Server.Atmos.Components;

// ReSharper disable once CheckNamespace // Partial of upstream system
namespace Content.Server.Atmos.EntitySystems;

public sealed partial class BarotraumaSystem
{
    public void RefreshPressureProtection(Entity<BarotraumaComponent?> ent)
    {
        if (Resolve(ent, ref ent.Comp, false))
            UpdateCachedResistances(ent, ent.Comp);
    }
}
