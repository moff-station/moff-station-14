using System.Diagnostics;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._Moffstation.MoffPatreon;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;

namespace Content.Server._Moffstation.MoffPatreon;

public sealed partial class MoffPatreonEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private readonly MoffPatreonSystem _patreonSystem;
    private readonly ISawmill _sawmill;

    public MoffPatreonEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("moffpatreon");
        _patreonSystem = _entity.System<MoffPatreonSystem>();
    }

    public override void Opened()
    {
        base.Opened();
        _adminManager.OnPermsChanged += OnPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();
        _adminManager.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
        }
    }

    public override EuiStateBase GetNewState() => new MoffPatreonEuiState
    {
        Patrons = _patreonSystem.GetPatrons().Select(p => p.Patron).ToList(),
        OnlinePlayers = _playerManager.Sessions.Select(s => s.Name).OrderBy(n => n).ToList(),
    };

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
            return;

        switch (msg)
        {
            case MoffPatreonAddRequest add:
                HandleAdd(add);
                break;
            case MoffPatreonRemoveRequest remove:
                HandleRemove(remove);
                break;
        }
    }

    private async void HandleAdd(MoffPatreonAddRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrId))
            return;

        var ids = request.UsernameOrId.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var failed = await _patreonSystem.AddPatrons(ids, Player).ToListAsync();

        if (failed.Count > 0)
        {
            _sawmill.Warning($"Failed to add patron(s): {string.Join(", ", failed)}");
        }

        SendMessage(new MoffPatreonAddResponse
        {
            Failed = failed,
            Successful = ids.Length - failed.Count,
        });
        StateDirty();
    }

    private async void HandleRemove(MoffPatreonRemoveRequest request)
    {
        var success = await _patreonSystem.RemovePatron(request.UserId);
        SendMessage(new MoffPatreonRemoveResponse { Success = success });
        StateDirty();
    }
}
