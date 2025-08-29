using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Abilities.Components;

/// <summary>
/// This component gives the entity it is attached to the Feed ability.
/// This is intended for Vampires but could in the future be used in conjunction with other users of <see cref="Vampire.Components.BloodEssenceUserComponent"/>
/// </summary>
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

    /// <summary>
    /// The sound that plays at the beginning of the feed do-after.
    /// </summary>
    [DataField]
    public SoundSpecifier FeedStartSound = new SoundCollectionSpecifier("retractor");

    /// <summary>
    /// The sound that plays after a successful feed attempt (the do-after wasn't cancelled)
    /// </summary>
    [DataField]
    public SoundSpecifier FeedSuccessSound = new SoundCollectionSpecifier("blood");

    /// <summary>
    /// The prototype of the action entity for this component to give to the entity.
    /// </summary>
    [DataField]
    public EntProtoId ActionProto = "ActionVampireFeed";

    /// <summary>
    /// A place to store the action entity.
    /// </summary>
    [DataField]
    public EntityUid? Action;

    public override bool SendOnlyToOwner => true;
}
