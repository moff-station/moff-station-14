using Content.Shared._Moffstation.GaussFabricator;

namespace Content.Client._Moffstation.GaussFabricator;

public sealed partial class GaussFabricatorSystem : SharedGaussFabricatorSystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;

    [SubscribeLocalEvent]
    private void OnAfterState(Entity<GaussFabricatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<GaussFabricatorComponent> ent)
    {
        if (_uiSystem.TryGetOpenUi(ent.Owner, GaussFabricatorUiKey.Key, out var bui))
            bui.Update();
    }
}
