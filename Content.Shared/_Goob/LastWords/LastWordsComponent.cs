namespace Content.Shared._Goob.LastWords;

/// <summary>
/// Tracks the last words a user has said.
/// </summary>
[RegisterComponent]
public sealed partial class LastWordsComponent : Component
{
    [DataField]
    public string? LastWords;
}
