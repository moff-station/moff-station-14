using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.CartridgeLoader.Cartridges;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SOSCartridgeComponent : Component
{
    [DataField]
    //Path to the id container
    public const string PDAIdContainer = "PDA-id";

    [DataField]
    //Name to use if no id is found
    public LocId DefaultName = "sos-caller-defaultname";

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDefaultName => Loc.GetString(DefaultName);

    [DataField]
    //Notification message
    public LocId HelpMessage = "sos-message";

    /// <summary>
    /// Message that gets played locally when the SoS button is used
    /// </summary>
    [DataField]
    public LocId NotificationMessage = "sos-notification-message";

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedNotificationMessage => Loc.GetString(NotificationMessage);

    /// <summary>
    /// Message that gets played locally when the SoS button is used
    /// </summary>
    [DataField]
    public LocId CooldownMessage = "sos-notification-cooldown-message";

    [DataField]
    //Channel to notify
    public ProtoId<RadioChannelPrototype> HelpChannel = "Security";

    [DataField, AutoPausedField]
    //Timeout between calls
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField]
    //Countdown until next call is allowed
    public TimeSpan NextUse = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool CanCall => NextUse <= TimeSpan.Zero;
}
