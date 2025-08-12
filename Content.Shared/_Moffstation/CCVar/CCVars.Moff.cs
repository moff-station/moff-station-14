using Robust.Shared.Configuration;

namespace Content.Shared._Moffstation.CCVar;

[CVarDefs]
public sealed class MoffCCVars
{
    /// <summary>
    /// Respawn time, how long the player has to wait in seconds after death. Set this to zero to disable respawning.
    /// </summary>
    public static readonly CVarDef<bool> RespawningEnabled =
        CVarDef.Create("moff.respawn_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Respawn time, how long the player has to wait in seconds after death. Set this to zero to disable respawning.
    /// </summary>
    public static readonly CVarDef<float> RespawnTime =
        CVarDef.Create("moff.respawn_time", 600f, CVar.SERVER | CVar.REPLICATED);
}
