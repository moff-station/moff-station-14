using Robust.Shared.Configuration;

namespace Content.Shared._AXOLOTL;

[CVarDefs]
public sealed class AxolotlCVars
{
        /// <summary>
        /// Respawn time, how long the player has to wait in seconds after death. Set this to zero to disable respawning.
        /// </summary>
        public static readonly CVarDef<float> RespawnTime =
            CVarDef.Create("game.respawn_time", 300.0f, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// The number of players that must exist on the server for the respawn button to be disabled.
        /// </summary>
        public static readonly CVarDef<int> MaxPlayersForRespawnButton =
            CVarDef.Create("game.max_players_for_respawn_button", 30, CVar.SERVER | CVar.REPLICATED);
}
