using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Physics;

/// <summary>
/// This is a version of <see cref="JointVisualsComponent"/> that can be used to display multiple joints at once.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiJointVisualsComponent : Component
{
    /// <summary>
    /// Sprite used when the line is drawn
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier Sprite = default!;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid?> Targets = [];

    [DataField, AutoNetworkedField]
    public Vector2 SourceOffset;

    [DataField, AutoNetworkedField]
    public Vector2 TargetOffset;
}
