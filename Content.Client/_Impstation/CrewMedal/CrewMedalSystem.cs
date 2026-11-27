using Content.Client._Impstation.CrewMedal.UI;
using Content.Shared._Impstation.CrewMedal;

namespace Content.Client._Impstation.CrewMedal;

public sealed partial class CrewMedalSystem : SharedCrewMedalSystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [SubscribeLocalEvent]
    private void OnCrewMedalAfterState(Entity<CrewMedalComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_uiSystem.TryGetOpenUi<CrewMedalBoundUserInterface>(ent.Owner, CrewMedalUiKey.Key, out var bui))
            return;

        bui.Reload();
    }
}
