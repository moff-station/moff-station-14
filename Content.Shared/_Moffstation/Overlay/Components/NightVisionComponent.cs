using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Overlay.Components;

/// When this component is on a player-controlled entity, it applies a night vision visual effect
/// (<see cref="Content.Client._Starlight.Overlay.NightVisionOverlay"/>) and creates a
/// <see cref="EffectPrototype">client-only effect entity</see> with a pointlight to illuminate the area around the entity.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class NightVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId EffectPrototype = "EffectNightVision";

    /// <summary>
    /// How much should night vision light up?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightBoost = 3.5f;

    /// <summary>
    /// How much should night vision light up?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightThreshold = 0.2f;

    /// <summary>
    /// What should the color of the tint be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color TintColor = Color.Green;

    /// <summary>
    /// What should the intensity of the tint be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TintIntensity = 0.85f;
}
