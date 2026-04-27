using Content.Shared.Body;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Medical.CryoCapsule;

/// <summary>
/// This is used for entities placed inside a CryoCapsule.
/// </summary>
[RegisterComponent]
public sealed partial class CryoCapsuleComponent : Component
{
    /// <summary>
    /// What organ types can be contained inside the capsule
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> CanContain;

    /// <summary>
    /// Organs currently inside the capsule (associated to their type)
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> Organs = new();

    /// <summary>
    /// Sound emitted when adding or removing organs inside the capsule.
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractSound;
}
