using Content.Shared._Moffstation.Atmos.Components;
using Content.Shared._Moffstation.Atmos.EntitySystems;
using Content.Shared._Moffstation.Atmos.Visuals;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SprayPainter.Components;
using Robust.Shared.Serialization;

// NOT a moffstation namespace because this partial class appends behavior to an existing class.
namespace Content.Shared.SprayPainter;

public abstract partial class SharedSprayPainterSystem
{
    [Dependency] private readonly GasTankVisualsSystem _gasTankVisuals = default!;

    private void InitializeGasTankPainting()
    {
        SubscribeLocalEvent<SprayPainterComponent, ComponentInit>(OnPainterInit);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterGasTankDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<GasTankVisualsComponent, InteractUsingEvent>(OnInteractUsing);
        Subs.BuiEvents<SprayPainterComponent>(SprayPainterUiKey.Key,
            subs =>
            {
                subs.Event<SprayPainterSetGasTankVisualsMessage>(OnPainterConfigUpdated);
            });
    }

    private void OnPainterInit(Entity<SprayPainterComponent> entity, ref ComponentInit args)
    {
        entity.Comp.GasTankVisuals = _gasTankVisuals.DefaultStyle;
    }

    private void OnDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterGasTankDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled ||
            args.Args.Target is not { } target)
            return;

        var painted = _gasTankVisuals.SetTankVisuals(target, ent.Comp.GasTankVisuals);
        if (!painted)
            return;

        Audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    private void OnPainterConfigUpdated(Entity<SprayPainterComponent> ent,
        ref SprayPainterSetGasTankVisualsMessage args)
    {
        if (args.Visuals.Equals(ent.Comp.GasTankVisuals))
            return;

        ent.Comp.GasTankVisuals = args.Visuals;
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnInteractUsing(Entity<GasTankVisualsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<SprayPainterComponent>(args.Used, out var painter))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            painter.GasTankSprayTime,
            new SprayPainterGasTankDoAfterEvent(),
            args.Used,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        if (!DoAfter.TryStartDoAfter(doAfterEventArgs, out _))
            return;

        args.Handled = true;

        // Log the attempt
        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} at {Transform(ent).Coordinates:targetlocation}");
    }
}

/// <summary>
/// This event is raised by the spray painter UI when selected gas tank visuals are changed.
/// </summary>
[Serializable, NetSerializable]
public sealed class SprayPainterSetGasTankVisualsMessage(GasTankVisuals visuals) : BoundUserInterfaceMessage
{
    public readonly GasTankVisuals Visuals = visuals;
}

[Serializable, NetSerializable]
public sealed partial class SprayPainterGasTankDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
