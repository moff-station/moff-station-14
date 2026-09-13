using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.EntitySystems;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Charges.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SprayPainter.Components;
using Robust.Shared.Serialization;

// NOT a moffstation namespace because this partial class appends behavior to an existing class.
namespace Content.Shared.SprayPainter;

// Additions to SharedSprayPainterSystem which are particular to GasHolderVisuals.
public abstract partial class SharedSprayPainterSystem
{
    [Dependency] private GasHolderVisualsSystem _gasHolderVisuals = default!;

    [Dependency] private EntityQuery<GasCanVisualsComponent> _gasCanVisualsQuery;
    [Dependency] private EntityQuery<GasTankVisualsComponent> _gasTankVisualsQuery;

    private void InitializeGasTankPainting()
    {
        Subs.BuiEvents<SprayPainterComponent>(
            SprayPainterUiKey.Key,
            subs => subs.Event<SprayPainterSetGasHolderVisualsMessage>(OnPainterConfigUpdated));
    }

    [SubscribeLocalEvent]
    private void OnPainterInit(Entity<SprayPainterComponent> entity, ref ComponentInit args)
    {
        // Initialize painters' configured visuals to the default.
        entity.Comp.GasHolderVisuals = _gasHolderVisuals.DefaultStyle;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterGasTankDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled ||
            args.Args.Target is not { } target)
            return;

        if (!(TrySetVisuals(_gasCanVisualsQuery) || TrySetVisuals(_gasTankVisualsQuery)))
            return;

        Charges.TryUseCharges(
            (ent, EnsureComp<LimitedChargesComponent>(ent)),
            ent.Comp.GasTankChargeCost
        );
        Audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        AdminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}"
        );

        args.Handled = true;
        return;

        bool TrySetVisuals<T>(EntityQuery<T> query) where T : Component, IGasHolderVisualsComponent =>
            query.ResolveOrNull(target, logMissing: false) is { } holder &&
            _gasHolderVisuals.TrySetTankVisuals<T>((holder, holder, null), ent.Comp.GasHolderVisuals);
    }

    private void OnPainterConfigUpdated(
        Entity<SprayPainterComponent> ent,
        ref SprayPainterSetGasHolderVisualsMessage args
    )
    {
        if (args.Visuals.Equals(ent.Comp.GasHolderVisuals))
            return;

        ent.Comp.GasHolderVisuals = args.Visuals;
        Dirty(ent);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<GasTankVisualsComponent> ent, ref InteractUsingEvent args) =>
        OnInteractUsing<GasTankVisualsComponent>(ent, ref args);

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<GasCanVisualsComponent> ent, ref InteractUsingEvent args) =>
        OnInteractUsing<GasCanVisualsComponent>(ent, ref args);

    private void OnInteractUsing<T>(Entity<T> ent, ref InteractUsingEvent args)
        where T : Component, IGasHolderVisualsComponent
    {
        if (args.Handled ||
            !TryComp<SprayPainterComponent>(args.Used, out var painter))
            return;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            painter.GasTankSprayTime,
            new SprayPainterGasTankDoAfterEvent(),
            args.Used,
            target: ent,
            used: args.Used
        )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        if (!DoAfter.TryStartDoAfter(doAfterEventArgs, out _))
            return;

        args.Handled = true;

        // Log the attempt
        AdminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} at {Transform(ent).Coordinates:targetlocation}"
        );
    }
}

/// <summary>
/// This event is raised by the spray painter UI when selected gas tank visuals are changed.
/// </summary>
[Serializable, NetSerializable]
public sealed class SprayPainterSetGasHolderVisualsMessage(GasHolderVisuals visuals) : BoundUserInterfaceMessage
{
    public readonly GasHolderVisuals Visuals = visuals;
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterGasTankDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
