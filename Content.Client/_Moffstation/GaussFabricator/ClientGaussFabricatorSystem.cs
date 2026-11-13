using Content.Shared._Moffstation.GaussFabricator;
using Content.Shared.Examine;

namespace Content.Client._Moffstation.GaussFabricator;

public sealed partial class ClientGaussFabricatorSystem : GaussFabricatorSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnAfterState(Entity<GaussFabricatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void ExamineAtmosphere(Entity<GaussFabricatorComponent> ent, ExaminedEvent args)
    {
    }

    protected override void SyncPower(Entity<GaussFabricatorComponent> ent)
    {
    }

    protected override void UpdateUi(Entity<GaussFabricatorComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, GaussFabricatorUiKey.Key, out var bui))
            bui.Update();
    }
}
