using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Vampire.Abilities.Components;

[RegisterComponent]
public sealed partial class AbilityFeedComponent : Component
{
    /// <summary>
    /// The duration of the feed action (in seconds)
    /// </summary>
    [DataField]
    public TimeSpan FeedDuration = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// The amount of blood to drink from someone per feed action
    /// </summary>
    [DataField]
    public float BloodPerFeed = 10.0f;

    [DataField]
    public EntProtoId ActionProto = "ActionVampireFeed";

    [DataField]
    public EntityUid? Action;
}
