using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class SosCartridgeComponent : Component
{
    [DataField]
    //Name to use if no id is found
    public string DefaultName = "sos-caller-defaultname";

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDefaultName => Loc.GetString(DefaultName);

    [DataField]
    //Notification message
    public string HelpMessage = "sos-message";

    [DataField]
    //Channel to notify
    public HashSet<ProtoId<RadioChannelPrototype>> HelpChannels =
    [
        "Security",
        "Medical"
    ];

    [DataField]
    //Timeout between calls
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField]
    //Countdown until next call is allowed
    public TimeSpan NextTime;
}
