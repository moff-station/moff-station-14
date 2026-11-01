using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.JestographicSequencer;

/// <summary>
///A special interaction between a jestographic sequencer and a target that has no access to reverse.
///All interactions are super neat in here :) - Resources/Prototypes/_Moffstation/JestographicSequencer/special_interactions.yml
/// </summary>
[Prototype]
public sealed partial class JestographicInteractionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///Targets matched by exact entity prototype.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = [];

    /// <summary>
    ///Targets matched by whitelist, in addition to <see cref="Prototypes"/>.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///Popup shown to the user. Receives the argument "target".
    /// </summary>
    [DataField(required: true)]
    public LocId Message;

    /// <summary>
    ///The target is replaced with this entity.
    /// </summary>
    [DataField]
    public EntProtoId? ReplaceWith;

    /// <summary>
    ///Change use delay.
    /// </summary>
    [DataField]
    public TimeSpan? UseDelay;

    /// <summary>
    ///Change speed multiplier.
    /// </summary>
    [DataField]
    public float? SpeedMultiplier;

    /// <summary>
    ///The target's sound intervals are faster/slower. Below 1 is faster.
    /// </summary>
    [DataField]
    public float? SoundIntervalMultiplier;

    /// <summary>
    ///The target recycler dresses mobs that fall into it in these unremovable items.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId>? RecyclerOutfit;

    /// <summary>
    ///Cuts MAP and AIV wires when applied to a camera.
    /// </summary>
    [DataField]
    public bool CutCameraWires;

    /// <summary>
    ///Allows this interaction to be used again after its effect is reversed.
    /// </summary>
    [DataField]
    public bool Repeatable;
}
