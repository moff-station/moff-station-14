using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.DeviceLinking.Components;

/// <summary>
/// Make an entity say a voice line when receiving a signal.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SignalSpeechComponent : Component
{
    /// <summary>
    /// Associate ports to voice line said when the port is triggered
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<SinkPortPrototype>, string> Lines = [];

    /// <summary>
    /// Port used to say a random voice line from the dictionary.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SinkPortPrototype>? RandomPort = "VoiceEmitterVoicelineRandom";

    /// <summary>
    /// Port being recorded (null : not recording)
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SinkPortPrototype>? RecordingPort;

    /// <summary>
    /// listening range when recording voice lines
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecordingRange = 4f;

    /// <summary>
    /// Minimum length for a voice line
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinLength = 3;

    /// <summary>
    /// Maximum length for a voice line
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxLength = 50;
}
