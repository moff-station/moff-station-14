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
        CVarDef.Create("moff.respawn_time", 900f, CVar.SERVER | CVar.REPLICATED);
}
