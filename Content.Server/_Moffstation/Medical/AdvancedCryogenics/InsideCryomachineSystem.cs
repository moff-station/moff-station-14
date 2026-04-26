using Content.Server.Body.Systems;
using Content.Server.Medical.Components;
using Content.Shared._Moffstation.Medical.AdvancedCryogenics;
using Content.Shared.Atmos;

namespace Content.Server._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles bodily functions of entities placed inside a Cryomachine.
/// </summary>
public sealed class InsideCryomachineSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<InsideCryomachineComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<InsideCryomachineComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<InsideCryomachineComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }

    private void OnInhaleLocation(Entity<InsideCryomachineComponent> ent, ref InhaleLocationEvent args)
    {
        if (ent.Comp.Cryomachine is not { } cryomachine ||
            !TryComp<CryoPodAirComponent>(cryomachine, out var machineAir))
            return;

        args.Gas = machineAir.Air;
    }

    private void OnExhaleLocation(Entity<InsideCryomachineComponent> ent, ref ExhaleLocationEvent args)
    {
        if (ent.Comp.Cryomachine is not { } cryomachine ||
            !TryComp<CryoPodAirComponent>(cryomachine, out var machineAir))
            return;

        args.Gas = machineAir.Air;
    }

    private void OnGetAir(Entity<InsideCryomachineComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (ent.Comp.Cryomachine is not { } cryomachine ||
            !TryComp<CryoPodAirComponent>(cryomachine, out var machineAir))
            return;

        args.Gas = machineAir.Air;
        args.Handled = true;
    }
}
