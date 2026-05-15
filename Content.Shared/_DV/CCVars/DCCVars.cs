using Robust.Shared.Configuration;

namespace Content.Shared._DV.CCVars;

/// <summary>
/// DeltaV specific cvars.
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming - Shush you
public sealed partial class DCCVars
{
    /// <summary>
    /// Whether the screenshake ported from ES should be disabled.
    /// False by default, so enabled. Players can change this in accessiblity settings.
    /// </summary>
    public static readonly CVarDef<bool> EsScreenshakeDisabled =
        CVarDef.Create("deltav.es_screenshake.disabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
