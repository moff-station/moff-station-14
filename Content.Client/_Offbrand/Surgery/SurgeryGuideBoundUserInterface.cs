using Content.Shared._Offbrand.Surgery;
using Content.Shared.Construction.Prototypes;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Offbrand.Surgery;

public sealed class SurgeryGuideBoundUserInterface : BoundUserInterface
{
    private SurgeryGuideMenu? _menu;

    public SurgeryGuideBoundUserInterface(EntityUid owner, Enum key) : base(owner, key)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SurgeryGuideMenu>();
        _menu.OnSurgerySelected += OnSurgerySelected;
        _menu.OnCleanUp += OnCleanUp;
    }

    private void OnSurgerySelected(ProtoId<ConstructionPrototype> surgery)
    {
        SendPredictedMessage(new SurgeryGuideStartSurgeryMessage(surgery));
    }

    private void OnCleanUp()
    {
        SendPredictedMessage(new SurgeryGuideStartCleanupMessage());
    }
}
