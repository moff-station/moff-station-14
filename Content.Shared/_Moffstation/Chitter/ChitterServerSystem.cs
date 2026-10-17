using Content.Shared._Moffstation.BladeServer;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Chitter;

/// This system implements the behavior of <see cref="ChitterServerComponent"/>. Mostly this is operations to interact
/// with servers from clients.
public sealed partial class ChitterServerSystem : EntitySystem
{
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private SharedBladeServerSystem _bladeServer = default!;
    [Dependency] private ChitterIdentitySystem _chitterIdentity = default!;
    [Dependency] private ChitterClientSystem _chitterClient = default!;
    [Dependency] private IComponentFactory _compFact = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;

    private const int MessageCharLimit = 500;
    private const int ChatNameCharLimit = 50;
    private const int MaxChatParticipants = 20;

    [SubscribeLocalEvent]
    private void OnEmagged(Entity<ChitterServerComponent> server, ref GotEmaggedEvent args)
    {
        ChitterServerEmagEffectComponent? effectComp = null;
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) ||
            _emag.CheckFlag(server, EmagType.Interaction) ||
            !_entityTable.GetSpawns(server.Comp.EmagEffects).TryFirstOrNull(out var protoId) &&
            ProtoMan.Resolve(protoId, out var proto) &&
            !proto.TryComp(out effectComp, _compFact) ||
            !ProtoMan.Resolve(effectComp?.Effect, out var effect)
           )
            return;

        server.Comp.CurrentEmagEffect = effect.ID;
        if (effect.ServerColor is { } color)
        {
            // _bladeServer.SetStripeColorOverride(server.Owner, color); // TODO CENT Not implemented yet
        }

        DirtyField(server.AsNullable(), nameof(ChitterServerComponent.Accounts));
        UpdateRecipients(server, server.Comp.Accounts.Keys);

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("chitter-server-emagged"), server, args.UserUid);
    }
}
