using Content.Client.UserInterface.Fragments;
using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.CartridgeLoader.Cartridges;

public sealed partial class ChitterPiUi : UIFragment
{
    private ChitterPiUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new ChitterPiUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is ChitterPiUiState cast)
            _fragment?.UpdateState(cast);
    }
}
