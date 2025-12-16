using Content.Server.Speech.Components;
using Content.Shared._Moffstation.Speech;
using Content.Shared.Alert;

namespace Content.Server._Moffstation.Speech;

public sealed class FrenchAccentToggleSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FrenchAccentToggleComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<FrenchAccentToggleComponent, ComponentRemove>(OnCompRemoved);
        SubscribeLocalEvent<FrenchAccentToggleComponent, ToggleFrenchAccentEvent>(OnToggleAccent);
    }

    private void OnCompInit(Entity<FrenchAccentToggleComponent> ent, ref ComponentInit args)
    {
        EnsureComp<FrenchAccentComponent>(ent);
        _alertsSystem.ShowAlert(ent.Owner, ent.Comp.ToggleAlertProtoId, 1);
    }

    private void OnCompRemoved(Entity<FrenchAccentToggleComponent> ent, ref ComponentRemove args)
    {
        RemComp<FrenchAccentComponent>(ent);
        _alertsSystem.ClearAlert(ent.Owner, ent.Comp.ToggleAlertProtoId);
    }

    private void OnToggleAccent(Entity<FrenchAccentToggleComponent> ent, ref ToggleFrenchAccentEvent args)
    {
        if (args.Handled)
            return;

        if (!RemComp<FrenchAccentComponent>(ent))
        {
            EnsureComp<FrenchAccentComponent>(ent);
            _alertsSystem.ShowAlert(ent.Owner, ent.Comp.ToggleAlertProtoId, 1);
        }
        else {
            _alertsSystem.ShowAlert(ent.Owner, ent.Comp.ToggleAlertProtoId, 0);
        }

        args.Handled = true;
    }
}
