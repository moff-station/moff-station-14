using Content.Client.UserInterface.Fragments;
using Content.Shared._Moffstation.Chitter.Probe;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Chitter.Probe;

[UsedImplicitly] // Referenced from YAML
public sealed partial class ChitterProbeUi : UIFragment
{
    private ChitterProbeView? _fragment;

    public override Control GetUIFragmentRoot() => _fragment!;

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner) =>
        _fragment = new ChitterProbeView();

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is ChitterProbeUiState cast)
            _fragment?.UpdateState(cast);
    }
}
