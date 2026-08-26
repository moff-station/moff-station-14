using Content.Shared._ES.Voting.Components;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Shared._Moffstation.Voting.Systems;

/// <summary>
/// Handles the alert used to toggle vote notifications on <see cref="ESVoterComponent"/>.
/// </summary>
public sealed partial class MoffVoteNotificationSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ESVoterComponent> ent, ref MapInitEvent args)
    {
        ShowAlert(ent);
    }

    [SubscribeLocalEvent]
    private void OnCompRemoved(Entity<ESVoterComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.AlertId);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<ESVoterComponent> ent, ref ToggleVoteNotificationEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.NotificationsEnabled = !ent.Comp.NotificationsEnabled;
        Dirty(ent);
        ShowAlert(ent);

        args.Handled = true;
    }

    private void ShowAlert(Entity<ESVoterComponent> ent)
    {
        _alerts.ShowAlert(ent.Owner, ent.Comp.AlertId, (short)(ent.Comp.NotificationsEnabled ? 1 : 0));
    }
}

/// <summary>
/// Event raised to toggle vote notifications on <see cref="ESVoterComponent"/>.
/// </summary>
[PublicAPI] // Referenced in YAML
public sealed partial class ToggleVoteNotificationEvent : BaseAlertEvent;
