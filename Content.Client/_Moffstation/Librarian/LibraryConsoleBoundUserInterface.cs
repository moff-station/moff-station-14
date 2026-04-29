using Robust.Client.UserInterface;

namespace Content.Client._Moffstation.Librarian;

public sealed class LibraryConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private LibraryConsoleMenu? _menu;

    public LibraryConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<LibraryConsoleMenu>();
    }
}
