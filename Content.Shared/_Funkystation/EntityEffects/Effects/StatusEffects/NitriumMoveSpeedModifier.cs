using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Funkystation.EntityEffects.Effects.StatusEffects;

public sealed partial class NitriumMovespeedModifier : BaseStatusEntityEffect<NitriumMovespeedModifier>
{
    [DataField]
    public float WalkSpeedModifier = 1f;

    [DataField]
    public float SprintSpeedModifier = 1f;

    /// <summary>
    /// Base duration in seconds before the modifier expires.
    /// Scaled by metabolism amount in Effect().
    /// </summary>
    [DataField]
    public float StatusLifetime = 6f;

    [DataField]
    public EntProtoId EffectProto = MovementModStatusSystem.ReagentSpeed;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-movespeed-modifier",
            ("chance", Probability),
            ("walkspeed", WalkSpeedModifier),
            ("sprintspeed", SprintSpeedModifier),
            ("time", StatusLifetime));
    }
}
