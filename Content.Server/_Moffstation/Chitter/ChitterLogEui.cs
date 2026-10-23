using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._Moffstation.Chitter;

/// This system manages and gates opening/closing of <see cref="ChitterLogEui"/>s.
public sealed partial class ChitterLogEuiSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminMan = default!;
    [Dependency] private EuiManager _euiMan = default!;

    [Dependency] private ChitterServerSystem _chitterServer = default!;

    private const AdminFlags RequiredPermissions = AdminFlags.Logs;

    private readonly Dictionary<ICommonSession, ChitterLogEui> _openEuis = [];

    public override void Initialize()
    {
        base.Initialize();
        _adminMan.OnPermsChanged += OnPermsChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _adminMan.OnPermsChanged -= OnPermsChanged;
    }

    /// Opens the EUI for the given session. Does nothing if the given session lacks <see cref="RequiredPermissions"/>.
    public void Open(ICommonSession session)
    {
        if (_openEuis.ContainsKey(session) ||
            !_adminMan.HasAdminFlag(session, RequiredPermissions))
            return;

        var eui = new ChitterLogEui(
            GetState,
            () => Close(session)
        );
        _openEuis[session] = eui;
        _euiMan.OpenEui(eui, session);
        eui.StateDirty();
    }

    /// Closes the EUI for the given session.
    public void Close(ICommonSession session)
    {
        if (_openEuis.Remove(session, out var eui))
            eui.Close();
    }

    [SubscribeLocalEvent]
    private void UpdateEuis(ref DirtyChitterEvent args)
    {
        foreach (var eui in _openEuis.Values)
        {
            eui.StateDirty();
        }
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        foreach (var eui in _openEuis.Values)
        {
            if (args.Player == eui.Player && !_adminMan.HasAdminFlag(eui.Player, AdminFlags.Logs))
                eui.Close();
        }
    }

    private ChitterLogEuiState GetState(ChitterChatId? selectedChat)
    {
        List<ChitterUiChatDetail> chats = [];
        List<ChitterAccount> accounts = [];
        foreach (var server in AllEntityQuery<ChitterServerComponent>())
        {
            chats.AddRange(_chitterServer.GetAllChats(server).Select(ChitterUiChatDetail.From));
            accounts.AddRange(_chitterServer.GetAllAccounts(server));
        }

        return new ChitterLogEuiState(selectedChat, chats, accounts);
    }
}

/// An admin-facing EUI for viewing chitter messages independently of any client.
public sealed partial class ChitterLogEui(Func<ChitterChatId?, ChitterLogEuiState> getState, Action onClose) : BaseEui
{
    public ChitterChatId? SelectedChat;

    public override EuiStateBase GetNewState() => getState(SelectedChat);
    public override void Closed() => onClose();

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not ChitterLogEuiChatSelectedMessage message)
            return;

        SelectedChat = message.Chat;
        StateDirty();
    }
}
