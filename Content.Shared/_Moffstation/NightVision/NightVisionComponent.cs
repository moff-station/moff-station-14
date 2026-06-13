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

    /// <summary>
    /// Whether night vision is currently active. Can be toggled via actions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Action to grant when this component is on a mob directly (innate night vision).
    /// </summary>
    [DataField]
    public EntProtoId? ToggleActionInnate;

    [ViewVariables]
    public EntityUid? ToggleActionInnateEntity;

    /// <summary>
    /// Action to grant when this component is on an equipped item.
    /// </summary>
    [DataField]
    public EntProtoId? ToggleActionEquipped;

    [ViewVariables]
    public EntityUid? ToggleActionEquippedEntity;
}
