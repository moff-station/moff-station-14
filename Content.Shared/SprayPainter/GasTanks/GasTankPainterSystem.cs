using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.GasTanks;

[Serializable, NetSerializable]
public sealed class GasTankPainterConfigUpdateMessage(GasTankVisuals visuals) : BoundUserInterfaceMessage
{
    public readonly GasTankVisuals Visuals = visuals;
}

[Serializable, NetSerializable]
public sealed partial class GasTankPainterDoAfterEvent : DoAfterEvent
{
    public readonly GasTankVisuals Visuals;

    public GasTankPainterDoAfterEvent(GasTankVisuals visuals)
    {
        Visuals = visuals;
    }

    public override DoAfterEvent Clone() => this;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GasTankPainterSystem))]
public sealed partial class GasTankPainterComponent : Component
{
    /// <summary>
    /// The sound to play when the airlock style is changed.
    /// </summary>
    [DataField]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// The duration of the do after for using this entity to change the style of the airlock.
    /// </summary>
    [DataField]
    public TimeSpan SprayTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public GasTankVisuals ConfiguredVisuals;
}

public sealed class GasTankPainterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    private static GasTankVisuals _default;
    private static ProtoId<GasTankVisualStylePrototype> _defaultProto = "Default";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasTankVisualsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GasTankPainterComponent, ComponentInit>(OnPainterInit);
        SubscribeLocalEvent<GasTankPainterComponent, GasTankPainterDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<GasTankVisualsComponent, InteractUsingEvent>(OnInteractUsing);
        Subs.BuiEvents<GasTankPainterComponent>(SprayPainterUiKey.Key,
            subs =>
            {
                subs.Event<GasTankPainterConfigUpdateMessage>(OnPainterConfigUpdated);
            });
    }

    public bool SetTankVisuals(Entity<GasTankVisualsComponent?, AppearanceComponent?> entity, GasTankVisuals visuals)
    {
        if (!Resolve(entity, ref entity.Comp1) ||
            !Resolve(entity, ref entity.Comp2))
            return false;

        entity.Comp1.Visuals = visuals;
        _appearance.SetData(entity, GasTankVisualsLayers.Tank, visuals.TankColor, entity.Comp2);
        _appearance.SetData((entity, entity.Comp2), GasTankVisualsLayers.StripeMiddle, visuals.MiddleStripeColor);
        _appearance.SetData((entity, entity.Comp2), GasTankVisualsLayers.StripeLow, visuals.LowerStripeColor);

        return true;
    }

    private void OnInit(Entity<GasTankVisualsComponent> entity, ref ComponentInit args)
    {
        if (!_proto.TryIndex(entity.Comp.InitialVisuals, out var visuals))
            return;

        SetTankVisuals((entity, entity.Comp, null), visuals);
    }

    private void OnPainterInit(Entity<GasTankPainterComponent> entity, ref ComponentInit args)
    {
        if (!_proto.TryIndex(_defaultProto, out var def))
            return;

        entity.Comp.ConfiguredVisuals = def.Visuals;
    }

    private void OnDoAfter(Entity<GasTankPainterComponent> ent, ref GasTankPainterDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled ||
            args.Args.Target is not { } target)
            return;

        var painted = SetTankVisuals(target, ent.Comp.ConfiguredVisuals);
        if (!painted)
            return;

        _audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    private void OnPainterConfigUpdated(Entity<GasTankPainterComponent> ent, ref GasTankPainterConfigUpdateMessage args)
    {
        if (args.Visuals.Equals(ent.Comp.ConfiguredVisuals))
            return;

        ent.Comp.ConfiguredVisuals = args.Visuals;
        Dirty(ent);
    }

    private void OnInteractUsing(Entity<GasTankVisualsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<GasTankPainterComponent>(args.Used, out var painter))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            painter.SprayTime,
            new GasTankPainterDoAfterEvent(painter.ConfiguredVisuals),
            args.Used,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        if (!_doAfter.TryStartDoAfter(doAfterEventArgs, out _))
            return;

        args.Handled = true;

        // Log the attempt
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} at {Transform(ent).Coordinates:targetlocation}");
    }
}
