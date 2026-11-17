using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Physics;

[RegisterComponent]
public sealed partial class VisualLinksComponent : Component
{
    [DataField(required: true)]
    public SpriteSpecifier LinkSprite;

    [DataField]
    public Vector2 OffsetA;

    [DataField]
    public Vector2 OffsetB;

    [DataField, NonSerialized]
    public HashSet<EntityUid> Targets = [];
}
