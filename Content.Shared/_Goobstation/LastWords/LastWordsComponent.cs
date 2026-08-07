namespace Content.Shared._Goobstation.LastWords; // Moffstation - _Goob -> _Goobstation

/// <summary>
/// Tracks the last words a user has said.
/// </summary>
[RegisterComponent]
public sealed partial class LastWordsComponent : Component
{
    [DataField]
    public string? LastWords;
}
