using Content.Server.Station.Systems;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared._Moffstation.Chitter;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Chitter;

// AI-side equivalent of ChitterCartridgeSystem: the AI's held entity carries a ChitterAccountComponent directly
// (auto-assigned a number by ChitterAccountSystem's MapInit handler, same as any ID card) and this system wires
// its intrinsic Chitter UI to the shared ChitterServerSystem API, without any PDA/ID card indirection. The actual
// message handling and UI-state building is shared with ChitterCartridgeSystem via ChitterUiMessageHandler.
public sealed partial class ChitterAiSystem : EntitySystem
{
    private const string JobName = "job-name-station-ai";

    [Dependency] private ChitterServerSystem _server = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private ChitterHandlerDeps Deps => new(_server, _station, _timing, _prototypeManager, EntityManager);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChitterAiComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ChitterAiComponent, ChitterAiUiMessageEvent>(OnMessage);

        // Refresh every AI's panel whenever anything actually changes on any server, instead of
        // polling all of them on a timer regardless of whether there's anything new to show.
        _server.DataChanged += OnServerDataChanged;
    }

    private void OnServerDataChanged()
    {
        var query = EntityQueryEnumerator<ChitterAiComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateUi((uid, comp));
        }
    }

    private void OnUiOpened(Entity<ChitterAiComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnMessage(Entity<ChitterAiComponent> ent, ref ChitterAiUiMessageEvent msg)
    {
        var discoverContacts = msg.Type is ChitterUiMessageType.RefreshContacts;

        var aiEntity = ent.Owner;
        ChitterUiMessageHandler.HandleMessage(
            ent.Comp,
            msg,
            (bool requirePower, out Entity<ChitterServerComponent> server, out Entity<ChitterAccountComponent> account, out string name, out string jobTitle)
                => TryGetContext(aiEntity, requirePower, out server, out account, out name, out jobTitle),
            Deps);

        UpdateUi(ent, discoverContacts);
    }

    // Resolves "who is asking" for the AI: its own held entity carries the ChitterAccountComponent
    // directly, and its display name/job are always the same regardless of server reachability.
    private bool TryGetContext(
        EntityUid aiEntity,
        bool requirePower,
        out Entity<ChitterServerComponent> server,
        out Entity<ChitterAccountComponent> account,
        out string name,
        out string jobTitle)
    {
        server = default;
        account = default;
        name = Name(aiEntity);
        jobTitle = Loc.GetString(JobName);

        var found = requirePower
            ? _server.TryFindServer(aiEntity, out server)
            : _server.TryFindServerAnyPower(aiEntity, out server);

        if (!found)
            return false;

        if (!TryComp<ChitterAccountComponent>(aiEntity, out var foundAccount))
            return false;

        account = (aiEntity, foundAccount);
        return true;
    }

    private void UpdateUi(Entity<ChitterAiComponent> ent, bool discoverContacts = true)
    {
        var hasAccount = TryComp<ChitterAccountComponent>(ent, out var account);
        var serverOnline = _server.TryFindServer(ent, out var serverEnt);

        var state = new ChitterUiState
        {
            HasIdCard = hasAccount,
            ServerOnline = serverOnline,
        };

        if (hasAccount && account != null)
        {
            var ownerName = Name(ent);
            var ownerJobTitle = Loc.GetString(JobName);

            state.OwnNumber = account.AccountId;
            state.OwnProfilePicture = account.ProfilePictureId;
            state.OwnName = ownerName;
            state.OwnJob = ownerJobTitle;

            if (serverOnline)
            {
                ChitterUiMessageHandler.PopulateState(
                    state,
                    serverEnt,
                    account.AccountId,
                    ownerName,
                    ownerJobTitle,
                    account.ProfilePictureId,
                    ent.Comp.CurrentChatId,
                    discoverContacts,
                    ent.Owner,
                    Deps);

                // PopulateState just (re-)registered our own account too, which disguises it the same
                // as everyone else's if the server's been emagged - reflect that back onto what we show
                // ourselves instead of the real identity computed above.
                if (_server.GetAccount(serverEnt.Comp, account.AccountId) is { } ownAccount)
                {
                    state.OwnName = ownAccount.Name;
                    state.OwnJob = ownAccount.JobTitle;
                    state.OwnProfilePicture = ownAccount.ProfilePictureId;
                }
            }
        }

        _ui.SetUiState(ent.Owner, ChitterAiUiKey.Key, state);
    }
}
