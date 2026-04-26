using System.IO.Compression;
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

[Serializable]
public struct PlayerBook
{
    public string Name = default!;
    public string Description = default!;
    public string Author = default!;
    public string Content = default!;

    internal PlayerBook(string name, string description, string author, string content)
    {
        Name = name;
        Description = description;
        Author = author;
        Content = content;
    }
}
