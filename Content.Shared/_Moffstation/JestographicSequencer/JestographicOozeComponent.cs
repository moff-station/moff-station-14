using Content.Shared.Access;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.JestographicSequencer;

/// <summary>
///Ooze left on a basic device by a jestographic sequencer. Visible on examine.
///Cleaning it off with soap restores what the sequencer changed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JestographicOozeComponent : Component
{
    ///<summary>
    ///Access lists from before the swap. Null if the device was only bolted.
    ///</summary>
    [DataField, AutoNetworkedField]
    public List<HashSet<ProtoId<AccessLevelPrototype>>>? AccessLists;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> DenyTags = [];

    [DataField, AutoNetworkedField]
    public bool Bolted;

    [DataField, AutoNetworkedField]
    public bool AiDisabled;

    [DataField]
    public TimeSpan CleanTime = TimeSpan.FromSeconds(30);
}

[Serializable, NetSerializable]
public sealed partial class JestographicCleanDoAfterEvent : SimpleDoAfterEvent;
