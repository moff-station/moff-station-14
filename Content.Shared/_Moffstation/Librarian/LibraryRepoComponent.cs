using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Librarian;

[RegisterComponent]
public sealed partial class LibraryRepoComponent : Component
{
    public List<PlayerBook> BookList;

    [Serializable, NetSerializable]
    public enum LibraryConsoleUiKey
    {
        Key
    }
}
