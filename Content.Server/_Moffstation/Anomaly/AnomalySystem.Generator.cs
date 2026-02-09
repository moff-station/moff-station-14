using Content.Server.Anomaly.Components;
using Content.Shared.Emag.Systems;
using Content.Server.Popups;

namespace Content.Server.Anomaly;

public sealed partial class AnomalySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    private void OnEmagged(EntityUid uid, AnomalyGeneratorComponent component, ref GotEmaggedEvent args)
    {
        if (args.Handled ||
            component.CooldownEndTime < Timing.CurTime ||
            HasComp<GeneratingAnomalyGeneratorComponent>(uid))
            return;

        component.CooldownEndTime = TimeSpan.Zero;
        _popup.PopupEntity(Loc.GetString("anomaly-generator-emagged", ("name", uid)), uid);
        UpdateGeneratorUi(uid, component);

        args.Handled = true;
    }
}
