using Content.Server.Bible.Components;
using Content.Shared._Funkystation.Chaplain;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Funkystation.Chaplain;

// <summary>
// System that allows the altar to be swapped with other altar styles
// </summary>
public sealed partial class AltarSwappingSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    private static readonly SoundSpecifier HolySound = new SoundPathSpecifier("/Audio/Effects/holy.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltarSwappingComponent, AfterInteractUsingEvent>(OnAltarInteracted);
        SubscribeLocalEvent<AltarSwappingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AltarSwappingComponent, BoundUserInterfaceMessageAttempt>(OnUiAttempt);

        Subs.BuiEvents<AltarSwappingComponent>(AltarSwappingUiKey.Key,
            subscriptions =>
            {
                subscriptions.Event<AltarSwappingSelectMessage>(OnStyleSelected);
            });
    }

    private void OnMapInit(Entity<AltarSwappingComponent> altar, ref MapInitEvent args)
    {
        var prototype = MetaData(altar).EntityPrototype;

        if (prototype == null)
            return;

        if (!AltarStyles.IsAvailableStyle(_prototypes, altar.Comp, prototype.ID))
            return;

        altar.Comp.CurrentStyle = prototype.ID;
        Dirty(altar);
    }

    private void OnAltarInteracted(Entity<AltarSwappingComponent> altar, ref AfterInteractUsingEvent args)
    {
        // Must be in reach
        if (args.Handled || !args.CanReach)
            return;

        // The item applied to the altar must be a bible
        if (!HasComp<BibleComponent>(args.Used))
            return;

        // The person using it must be an authorized bible user
        if (!HasComp<BibleUserComponent>(args.User))
            return;

        args.Handled = _ui.TryToggleUi(altar.Owner, AltarSwappingUiKey.Key, args.User);
    }

    private void OnUiAttempt(Entity<AltarSwappingComponent> altar, ref BoundUserInterfaceMessageAttempt args)
    {
        // Something something never trust the client, make sure the user is actually allowed to use the altar
        if (!HasComp<BibleUserComponent>(args.Actor))
            args.Cancel();
    }

    private void OnStyleSelected(Entity<AltarSwappingComponent> altar, ref AltarSwappingSelectMessage args)
    {
        // Log.Info($"Received altar selection: {args.Prototype}");
        if (!HasComp<BibleUserComponent>(args.Actor))
            return;

        if (!AltarStyles.IsAvailableStyle(_prototypes, altar.Comp, args.Prototype))
        {
            return;
        }

        if (!_prototypes.TryIndex(args.Prototype, out EntityPrototype? prototype))
        {
            return;
        }

        // track the old style for logging purposes
        var oldStyle = altar.Comp.CurrentStyle?.ToString() ?? "unknown";

        altar.Comp.CurrentStyle = args.Prototype;

        // make sure we properly update everything
        var metadata = MetaData(altar);
        _metaData.SetEntityName(altar, prototype.Name, metadata);
        _metaData.SetEntityDescription(altar, prototype.Description, metadata);

        Dirty(altar);

        _adminLogger.Add(
            LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} changed the appearance of " +
            $"{ToPrettyString(altar.Owner):entity} from {oldStyle} to {args.Prototype}");

        // Play sound and raise event to play particles
        _audio.PlayPvs(HolySound, altar.Owner);
        RaiseNetworkEvent(new AltarSwappedEffectEvent(GetNetEntity(altar.Owner)), Filter.Pvs(altar.Owner));
    }
}
