using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._Moffstation.Medical.AdvancedCryogenics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Server._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryomachineSystem : SharedCryomachineSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasAnalyzerSystem _gasAnalyzer = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CryomachineComponent, AtmosDeviceUpdateEvent>(OnUpdateAtmosphere);
        SubscribeLocalEvent<CryomachineComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);


        SubscribeLocalEvent<InsideCryomachineComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<InsideCryomachineComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<InsideCryomachineComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }

    private void OnUpdateAtmosphere(Entity<CryomachineComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNode(ent.Owner, ent.Comp.PortName, out PortablePipeNode? portNode))
            return;

        if (!TryComp(ent, out CryoPodAirComponent? cryoPodAir))
            return;

        _atmosphereSystem.React(cryoPodAir.Air, portNode);

        if (portNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
        {
            _gasCanisterSystem.MixContainerWithPipeNet(cryoPodAir.Air, net.Air);
        }
    }

    private void OnGasAnalyzed(Entity<CryomachineComponent> ent, ref GasAnalyzerScanEvent args)
    {
        if (!TryComp(ent, out CryoPodAirComponent? cryoPodAir))
                return;

        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(ent.Owner), cryoPodAir.Air));
        // If it's connected to a port, include the port side
        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_nodeContainer.TryGetNode(ent.Owner, ent.Comp.PortName, out PipeNode? port) && port.Air.Volume != 0f)
        {
            var portAirLocal = port.Air.Clone();
            portAirLocal.Multiply(port.Volume / port.Air.Volume);
            portAirLocal.Volume = port.Volume;
            args.GasMixtures.Add((ent.Comp.PortName, portAirLocal));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CryomachineComponent>();

        while (query.MoveNext(out var uid, out var cryomachine))
        {
            if (_time.CurTime < cryomachine.NextUiUpdate)
                continue;

            cryomachine.NextUiUpdate = _time.CurTime + cryomachine.UiUpdateInterval;
            Dirty(uid, cryomachine);
            UpdateUi((uid, cryomachine));
        }
    }

    public void UpdateUi(Entity<CryomachineComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, CryomachineUiKey.Key) ||
            !TryComp(ent, out CryoPodAirComponent? air))
            return;

        if (ent.Comp.CapsuleSlot.Item is not { } capEnt ||
            !TryComp(capEnt, out CryocapsuleComponent? cap))
            return;

        if (!ent.Comp.CapsuleSlot.HasItem ||
            !TryComp(ent.Comp.CapsuleSlot.Item, out CryocapsuleComponent? capsule))
            return;

        //var capsule = ent.Comp.CapsuleSlot.ContainerSlot?.ContainedEntity;
        var gasMix = _gasAnalyzer.GenerateGasMixEntry("Cryo pod", air.Air);
        var cryoCapsule = _cryoCapsule.GenerateCryocapsuleEntry((capEnt, cap));
        var cryoCapsuleNetEnt = _entityManager.GetNetEntity(capEnt);
        var (beakerCap, beaker) = GetBeakerInfo(ent);

        _ui.ServerSendUiMessage(
            ent.Owner,
            CryomachineUiKey.Key,
            new CryomachineUiState(gasMix, cryoCapsule, cryoCapsuleNetEnt, beakerCap, beaker));
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
