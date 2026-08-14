using Content.Shared.Verbs;

namespace Content.Shared._Moffstation.Verbs;

/// <summary>
/// Moffstation verb categories, kept out of <see cref="VerbCategory"/> so upstream needs no edit.
/// </summary>
public static class MoffVerbCategory
{
    /// <summary>Groups the per-character "spawn as" entries, since a player has no single selected character.</summary>
    public static readonly VerbCategory Spawn =
        new("admin-player-actions-spawn", "/Textures/Interface/emotes.svg.192dpi.png");
}
