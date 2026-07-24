using Content.Shared.Radio.EntitySystems;
using Content.Shared.Chat;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._Goob.StationRadio.Systems; // Moffstation

namespace Content.Shared.Radio.Components;

/// <summary>
///     Listens for radio messages and relays them to local chat.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRadioDeviceSystem), typeof(StationRadioReceiverSystem))] // Moffstation - Add StationRadioReceiverSystem access.
public sealed partial class RadioSpeakerComponent : Component
{
    /// <summary>
    /// Whether or not interacting with this entity
    /// toggles it on or off.
    /// </summary>
    [DataField]
    public bool ToggleOnInteract = true;

    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new() { SharedChatSystem.CommonChannel };

    [DataField, AutoNetworkedField]
    public bool Enabled;

    // Moffstation - Start - Allow speech to be full volume
    [DataField, AutoNetworkedField]
    public bool LouderSpeech;
    // Moffstation - End

    // Goobstation - Start - Radio Host
    /// <summary>
    /// Does the radio need to be on a power grid to work?
    /// </summary>
    [DataField]
    public bool PowerRequired;
    // Goobstation - End - Radio Host
}
