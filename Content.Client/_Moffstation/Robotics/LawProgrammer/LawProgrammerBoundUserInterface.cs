using Content.Shared._Moffstation.Robotics.LawProgrammer;
using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Robotics.LawProgrammer;

public sealed class LawProgrammerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private LawProgrammerWindow? _window;

    public LawProgrammerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LawProgrammerWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not LawProgrammerBuiState casted)
            return;

        _window?.Update(casted);
    }
}
