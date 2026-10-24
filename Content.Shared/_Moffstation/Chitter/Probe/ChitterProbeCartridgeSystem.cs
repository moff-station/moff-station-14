using System.Collections.Immutable;
using System.Linq;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.CartridgeLoader;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Chitter.Probe;

/// This server implements the behavior of <see cref="ChitterProbeCartridgeComponent"/>.
public sealed partial class ChitterProbeCartridgeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private ChitterServerSystem _chitterServer = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<CartridgeLoaderComponent> _cartridgeLoaderQuery;
    [Dependency] private EntityQuery<ChitterServerComponent> _chitterServerQuery;

    [SubscribeLocalEvent]
    private void OnUiReady(Entity<ChitterProbeCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent, args.Loader.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(
        Entity<ChitterProbeCartridgeComponent> ent,
        ref CartridgeRelayedEvent<AfterInteractEvent> args
    )
    {
        if (args.Args.Handled ||
            !args.Args.CanReach ||
            _chitterServerQuery.ResolveOrNull(args.Args.Target) is not { } server)
            return;

        _doAfter.TryStartDoAfter(
            new DoAfterArgs(EntityManager,
                args.Args.User,
                ent.Comp.ScanDelay,
                new ChitterProbeScanDoAfterEvent(),
                ent.Owner,
                server,
                args.Loader
            )
            {
                BreakOnMove = true,
                NeedHand = true,
            }
        );

        args.Args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<ChitterProbeCartridgeComponent> entity, ref ChitterProbeScanDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled ||
            args.Args.Used is not { } used ||
            _chitterServerQuery.ResolveOrNull(args.Args.Target, logMissing: false) is not { } server)
            return;

        args.Handled = true;

        var serverName = Name(server);
        entity.Comp.ProbeData = new ChitterProbeData(
            serverName,
            null, // Initialize the current chat to null because we're on a new server.
            _chitterServer.GetAllChats(server).Select(ChitterUiChatDetail.From).ToImmutableArray(),
            _chitterServer.GetAllAccounts(server).ToImmutableArray(),
            _timing.CurTime
        );
        Dirty(entity);

        _audio.PlayPredicted(entity.Comp.ScanSound, used, args.Args.User);
        if (entity.Comp.ScanPopup is { } popup)
        {
            _popup.PopupEntity(
                Loc.GetString(popup, ("server", serverName)),
                used,
                args.Args.User
            );
        }

        UpdateUi(entity, used);
    }

    [SubscribeLocalEvent]
    private void OnMessage(Entity<ChitterProbeCartridgeComponent> entity, ref CartridgeMessageEvent a)
    {
        if (a is not ChitterProbeChatSelectedMessage args ||
            entity.Comp.ProbeData is not { } data)
            return;

        entity.Comp.ProbeData = data with { SelectedChat = args.Chat };
        Dirty(entity);
        UpdateUi(entity, GetEntity(args.LoaderUid));
    }

    private void UpdateUi(Entity<ChitterProbeCartridgeComponent> entity, Entity<CartridgeLoaderComponent?> loader) =>
        _cartridge.UpdateCartridgeUiState(loader, new ChitterProbeUiState(entity.Comp.ProbeData));
}
