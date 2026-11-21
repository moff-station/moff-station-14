using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Indicates an entity that has <see cref="StationAiHeldComponent"/> can interact with this.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStationAiSystem))]
public sealed partial class StationAiWhitelistComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    // Moffstation - AI Access always visible

    /// <summary>
    /// If true this device will always be visible to the AI when enabled (bypass overlay)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlwaysVisible = true;

    /// <summary>
    /// If true this device can be interacted with by the AI when enabled (bypass overlay)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlwaysAccessible = true;
    // Moffstation - end
}
