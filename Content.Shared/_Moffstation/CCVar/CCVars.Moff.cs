using Robust.Shared.Configuration;

namespace Content.Shared._Moffstation.CCVar;

[CVarDefs]
public sealed class MoffCCVars
{
    /// <summary>
    /// Whether the respawn button is available to ghost players
    /// </summary>
    public static readonly CVarDef<bool> RespawningEnabled =
        CVarDef.Create("moff.respawn_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Respawn time, how long the player has to wait in seconds after death. Set this to zero to disable timer.
    /// </summary>
    public static readonly CVarDef<float> RespawnTime =
        CVarDef.Create("moff.respawn_time", 450f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// The maximum size that an overlay stack can reach. Additional overlays will be superimposed over the last one.
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayShowWatchlist =
        CVarDef.Create("ui.admin_overlay_show_watchlist", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether longspeech should be enabled
    /// </summary>
    public static readonly CVarDef<bool> LongSpeech =
        CVarDef.Create("moff.long_speech", false, CVar.SERVER);

    /// <summary>
    ///     Blocks restart votes when the lobby is paused
    /// </summary>
    public static readonly CVarDef<bool> BlockRestartWhenPaused =
        CVarDef.Create("vote.block_restart_when_paused", true, CVar.SERVERONLY);

    /// <summary>
    ///     Makes map votes roll over until the map in question gets selected
    /// </summary>
    public static readonly CVarDef<bool> MapVotesRollOver =
        CVarDef.Create("votekick.map_votes_rollover", true, CVar.SERVERONLY);

    /// <summary>
    ///     Automatically starts a new map vote at the end of each round
    /// </summary>
    public static readonly CVarDef<bool> AutoStartMapVote =
        CVarDef.Create("votekick.auto_start_map_vote", true, CVar.SERVERONLY);

    /// <summary>
    ///     If false, prevents the previous played map from appearing in votes or being selected
    /// </summary>
    public static readonly CVarDef<bool> AllowDoublePickMap =
        CVarDef.Create("votekick.allow_double_pick_map", false, CVar.SERVERONLY);

    /// <summary>
    ///     How many maps appear in the map vote
    /// </summary>
    public static readonly CVarDef<int> MapVoteCount =
        CVarDef.Create("votekick.map_vote_count", 3, CVar.SERVERONLY);
}
