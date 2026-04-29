using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._Moffstation.Medical.CryoCapsule;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class CryoLifeSupportSystem : SharedCryoLifeSupportSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    [Dependency] private readonly GasAnalyzerSystem _gasAnalyzer = default!;
    [Dependency] private readonly NodeContainerSystem _node = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly GasCanisterSystem _gasCan = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    [Dependency] private readonly ChatSystem _chat = default!;

    /// <inheritdoc/>

    // probably not the way but. heh.
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoLifeSupportComponent, AtmosDeviceUpdateEvent>(OnAtmosDeviceUpdate);
        SubscribeLocalEvent<CryoLifeSupportComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CryoLifeSupportComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_time.CurTime < comp.UiNextUpdateTime)
                continue;

            comp.UiNextUpdateTime = _time.CurTime + comp.UiUpdateInterval;
            Dirty(uid, comp);
            UpdateUi((uid, comp));
        }
    }

    public void UpdateUi(Entity<CryoLifeSupportComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, CryoLifeSupportUiKey.Key) ||
            ! TryComp<CryoPodAirComponent>(ent, out var airComp))
            return;

        var gasMix = _gasAnalyzer.GenerateGasMixEntry("Cryogenic Life Support", airComp.Air);
        var (reagentCapacity, reagents) = GenerateReagentsEntry(ent);

        var capsuleEntity = ent.Comp.CapsuleSlot.Item;
        if (capsuleEntity is not { } capsule)
        {
            _ui.ServerSendUiMessage(ent.Owner,
                CryoLifeSupportUiKey.Key,
                new CryoLifeSupportUiState(gasMix, reagentCapacity, reagents, null, null));
            return;
        }

        var query = new OrganStatusQueryEvent(ent.Comp.MonitoredOrgans);
        RaiseLocalEvent(capsule, ref query);

        _ui.ServerSendUiMessage(ent.Owner,
            CryoLifeSupportUiKey.Key,
            new CryoLifeSupportUiState(
                gasMix,
                reagentCapacity,
                reagents,
                GetNetEntity(capsule),
                ent.Comp.MonitoredOrganNames.Zip(query.OrganEntries, (x,y) => (x,y)).ToList()));
    }

    private void OnAtmosDeviceUpdate(Entity<CryoLifeSupportComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!_node.TryGetNode(ent.Owner, ent.Comp.PortName, out PortablePipeNode? portNode) ||
            !TryComp(ent, out CryoPodAirComponent? cryoPodAir))
            return;

        _atmos.React(cryoPodAir.Air, portNode);

        if (portNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
        {
            _gasCan.MixContainerWithPipeNet(cryoPodAir.Air, net.Air);
        }
    }

    private void OnGasAnalyzed(Entity<CryoLifeSupportComponent> ent, ref GasAnalyzerScanEvent args)
    {
        if (!TryComp(ent, out CryoPodAirComponent? cryoPodAir))
            return;

        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(ent.Owner), cryoPodAir.Air));
        // If it's connected to a port, include the port side
        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_node.TryGetNode(ent.Owner, ent.Comp.PortName, out PipeNode? port) && port.Air.Volume != 0f)
        {
            var portAirLocal = port.Air.Clone();
            portAirLocal.Multiply(port.Volume / port.Air.Volume);
            portAirLocal.Volume = port.Volume;
            args.GasMixtures.Add((ent.Comp.PortName, portAirLocal));
        }
    }

    private (FixedPoint2? capacity, List<ReagentQuantity>?) GenerateReagentsEntry(Entity<CryoLifeSupportComponent> ent)
    {
        if (ent.Comp.BeakerSlot.Item is not { } beaker ||
            !TryComp<FitsInDispenserComponent>(beaker, out var fitsComp) ||
            !TryComp<SolutionContainerManagerComponent>(beaker, out var solComp) ||
            !_solution.TryGetFitsInDispenser((beaker, fitsComp, solComp), out var soIn, out _))
            return (null, null);

        var capacity = soIn.Value.Comp.Solution.MaxVolume;
        var reagents = soIn.Value.Comp.Solution.Contents
            .Select(reagent => new ReagentQuantity(reagent.Reagent, reagent.Quantity))
            .ToList();

        return (capacity, reagents);
    }

}
