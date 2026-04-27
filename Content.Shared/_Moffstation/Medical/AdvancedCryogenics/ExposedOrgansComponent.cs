using Content.Shared.Body;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for Entities with a <see cref="BodyComponent"/> containing organs
/// that can be placed and removed
/// </summary>
[RegisterComponent]
public sealed partial class ExposedOrgansComponent : Component
{
    /// <summary>
    /// The categories of the organs that are exposed.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExposedCategories;

    /// <summary>
    /// Sound that will be played when placing / removing an organ
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractionSound = new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg");

    /// <summary>
    /// The organ inside the body associated to each exposed category.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> Organs = new();
}
