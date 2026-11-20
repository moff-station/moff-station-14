using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Trigger.Components.Effects;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TalkOnTriggerComponent : Component
{
    /// <summary>
    /// Associate ports to voiceline said when the port is triggered
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<SinkPortPrototype>, string> Lines = [];

    /// <summary>
    /// Port used to say a random voiceline from the dictionary.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SinkPortPrototype>? RandomPort;

    /// <summary>
    /// Port being recorded (null : not recording)
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SinkPortPrototype>? RecordingPort;

    /// <summary>
    /// Range in which we listen when recording voice lines
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecordingRange = 4f;

    /// <summary>
    /// Minimum length for a voiceline
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinLength = 3;

    /// <summary>
    /// Maximum length for a voiceline
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxLength = 50;
}
