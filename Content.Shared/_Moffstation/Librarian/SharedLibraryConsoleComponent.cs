using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Librarian;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class SharedLibraryConsoleComponent : Component
{

    [Serializable, NetSerializable]
    public enum LibraryConsoleUiKey
    {
        Key
    }
}
