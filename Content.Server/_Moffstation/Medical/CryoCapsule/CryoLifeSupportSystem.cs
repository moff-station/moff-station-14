using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Medical.Components;
using Content.Shared._Moffstation.Medical.CryoCapsule;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class CryoLifeSupportSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly GasAnalyzerSystem _gasAnalyzer = default!;

    /// <inheritdoc/>

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
        var (reagentCapacity, reagents) = GenerateReagentsEntry();

        var capsuleEntity = ent.Comp.CapsuleSlot.Item;
        if (capsuleEntity is not { } capsule)
        {
            _ui.ServerSendUiMessage(ent.Owner,
                CryoLifeSupportUiKey.Key,
                new CryoLifeSupportUiState(gasMix, reagentCapacity, reagents, null, null));
            return;
        }

        // todo : annihilate this.

        var query = new OrganStatusQueryEvent(
            ent.Comp.MonitoredOrganNames.Zip(
                ent.Comp.MonitoredOrgans,
                (x, y) => (x, y))
                .ToList()
            );

        RaiseLocalEvent(capsule, ref query);

        _ui.ServerSendUiMessage(ent.Owner,
            CryoLifeSupportUiKey.Key,
            new CryoLifeSupportUiState(gasMix, reagentCapacity, reagents, GetNetEntity(capsule), query.OrganEntries));
    }

    private (FixedPoint2? capacity, List<ReagentQuantity>?) GenerateReagentsEntry()
    {
        // todo !
        return (null, null);
    }
}
