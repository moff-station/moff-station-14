using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._Moffstation.Chitter;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server._Moffstation.Chitter;

// Admin-only panel listing every Chitter conversation, live and archived, across every server.
// Opened via the "showchitterlog" command.
public sealed partial class ChitterLogEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IEntityManager _entity = default!;

    private readonly ChitterServerSystem _server;

    public ChitterLogEui()
    {
        IoCManager.InjectDependencies(this);
        _server = _entity.System<ChitterServerSystem>();
    }

    public override void Opened()
    {
        base.Opened();
        _adminManager.OnPermsChanged += OnPermsChanged;
        _server.DataChanged += StateDirty;
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();
        _adminManager.OnPermsChanged -= OnPermsChanged;
        _server.DataChanged -= StateDirty;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
            Close();
    }

    public override EuiStateBase GetNewState()
    {
        var chats = new List<ChitterLogChat>();

        foreach (var server in _server.GetAllServers())
        {
            chats.AddRange(ChitterServerSystem.BuildLogChats(server.Chats.Values, server));
            chats.AddRange(ChitterServerSystem.BuildLogChats(server.ArchivedChats.Values, server));
        }

        return new ChitterLogEuiState { Chats = chats };
    }
}
