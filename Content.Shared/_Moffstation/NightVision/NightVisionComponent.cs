using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.NightVision;

/// When this component is on a player-controlled entity, it applies a night vision visual effect
/// (<see cref="Content.Client._Starlight.Overlay.NightVisionOverlay"/>) and creates a
/// <see cref="EffectPrototype">client-only effect entity</see> with a pointlight to illuminate the area around the entity.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class NightVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId EffectPrototype = "EffectNightVision";

    /// The instance of <see cref="EffectPrototype"/>.
    [ViewVariables]
    public EntityUid? Effect;

    /// <summary>
    /// What should the color of the tint be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color TintColor = Color.FromHex("#00FF00");

    /// <summary>
    /// What should the intensity of the tint be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TintIntensity = 0.85f;
}
