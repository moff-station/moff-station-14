using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared._Moffstation.CCVar;

[CVarDefs]
public sealed class MoffCCVars
{
    /*
     * Admin
     */

    /// <summary>
    /// The maximum size that an overlay stack can reach. Additional overlays will be superimposed over the last one.
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayShowWatchlist =
        CVarDef.Create("ui.admin_overlay_show_watchlist", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Voting
     */

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
    ///     Automatically starts a map vote during the pre-round lobby
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

    /*
     * Gameplay
     */

    /// <summary>
    /// if true, the player count check for rules will be based on the number of players readied, versus the total number in the lobby.
    /// </summary>
    public static readonly CVarDef<bool>
        GameRulesCountReadied = CVarDef.Create("game.rules_count_readied", true, CVar.SERVERONLY);

    /// <summary>
    /// Whether longspeech should be enabled
    /// </summary>
    public static readonly CVarDef<bool> LongSpeech =
        CVarDef.Create("moff.long_speech", false, CVar.SERVER);

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
    /// Respawn time, how long the player has to wait in seconds after death. Set this to zero to disable timer.
    /// </summary>
    public static readonly CVarDef<float> MoffScreenShakeIntensity =
        CVarDef.Create("moff.screenshake_intensity", 1.0f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Arrivals
     */

    /// <summary>
    ///     Whether players should spawn at arrivals at the start of the round
    /// </summary>
    public static readonly CVarDef<bool> StartAtArrivals =
        CVarDef.Create("shuttle.start_at_arrivals", true, CVar.SERVERONLY);

    /// <summary>
    ///     If players start the round at arrivals, how long should it be until latejoins can enter from station cryosleep?
    /// </summary>
    public static readonly CVarDef<int> SpawnPreferenceDelay =
        CVarDef.Create("shuttle.spawn_preference_delay", 5, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum range people are allowed to travel from the center of the arrivals map
    /// </summary>
    public static readonly CVarDef<float> ArrivalsRange =
        CVarDef.Create("shuttle.arrivals_range", 50f, CVar.SERVERONLY);

    /*
     * Patreon
     */

    /// <summary>
    /// Whether to show or not show Moffstation Patreons special ooc color
    /// </summary>
    [CVarControl(AdminFlags.Server)]
    public static readonly CVarDef<bool> OocMoffPatronColorEnabled =
        CVarDef.Create("moff.ooc_moff_patron_color_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// What color Moffstation Patreons get in the OOC chat using Hex code
    /// </summary>
    [CVarControl(AdminFlags.Server)]
    public static readonly CVarDef<string> OocMoffPatronColor =
        CVarDef.Create("moff.ooc_moff_patron_color", "#aa00ff", CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether to show or not show Space Station 14 Patreons special ooc color
    /// </summary>
    [CVarControl(AdminFlags.Server)]
    public static readonly CVarDef<bool> OocUpstreamPatronColorEnabled =
        CVarDef.Create("moff.ooc_upstream_patron_color_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether a discord event should be created and managed as long as the round timer is unpaused
    /// false by default so the code doesnt try to run on localhost
    /// </summary>
    public static readonly CVarDef<bool> DiscordRoundEventEnabled =
        CVarDef.Create("moff.discord_round_event_enabled", false, CVar.SERVERONLY);

    /// <summary>
    /// The title of the discord event
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundEventName =
        CVarDef.Create("moff.discord_round_event_name", "Moffstation!", CVar.SERVERONLY);

    /// <summary>
    /// The description of the discord event (visible when clicked on)
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundEventDescription =
        CVarDef.Create("moff.discord_round_event_description", "Moff is up!\nFair warning: the end time is not accurate.", CVar.SERVERONLY);

    /// <summary>
    /// The location of the discord event (visible under the title)
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundEventLocation =
        CVarDef.Create("moff.discord_round_event_location", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Whether a pending update/uptime restart is allowed to trigger while the game is paused
    ///     with players still connected. Set to false to disable this behavior entirely.
    /// </summary>
    public static readonly CVarDef<bool> RestartQueueEnabled =
        CVarDef.Create("moff.restart_queue_enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     If an update is pending, but the round is paused and there are players sitting in the lobby
    ///     how long should we queue up a server restart in minutes?
    /// </summary>
    public static readonly CVarDef<int> RestartQueueTimer =
        CVarDef.Create("moff.restart_queue_timer", 30, CVar.SERVERONLY);

    /// <summary>
    ///     How often should players be notified when the server is restarting?
    /// </summary>
    public static readonly CVarDef<int> PauseRestartAnnounceInterval =
        CVarDef.Create("moff.restart_queue_announce_interval", 5, CVar.SERVERONLY);

    /// <summary>
    ///     Once below the <see cref="PauseRestartFinalAnnounceThreshold"/>, how often should players be notified when the server is restarting?
    /// </summary>
    public static readonly CVarDef<int> PauseRestartFinalAnnounceInterval =
        CVarDef.Create("moff.restart_queue_final_announce_interval", 1, CVar.SERVERONLY);

    /// <summary>
    ///     When should more-often reminders that the server is restarting appear?
    /// </summary>
    public static readonly CVarDef<int> PauseRestartFinalAnnounceThreshold =
        CVarDef.Create("moff.restart_queue_final_announce_threshold", 5, CVar.SERVERONLY);
}
