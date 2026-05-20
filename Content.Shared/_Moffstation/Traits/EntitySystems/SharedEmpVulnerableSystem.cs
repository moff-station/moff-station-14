using Content.Shared._Moffstation.Traits.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Jittering;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;


namespace Content.Shared._Moffstation.Traits.EntitySystems;

public abstract partial class SharedEmpVulnerableSystem: EntitySystem
{
    private static readonly ProtoId<SoundCollectionPrototype> Sparks = "sparks";

    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectNew.StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedJitteringSystem _jittering = default!;

    /// <summary>
    /// Subjects the entity to disruptive effects due to a recent ion storm
    /// </summary>
    public void IonStormTarget(Entity<EmpVulnerableComponent> ent)
    {
        Disrupt(ent, ent.Comp.IonStunDuration, false);
    }

    /// <summary>
    /// Blinds and Stuns/Slows the target for the given duration
    /// </summary>
    /// <param name="target">The entity to be affected</param>
    /// <param name="duration">The duration of both the blindness and stun/slow effect</param>
    /// <param name="doStun">If the target should be stunned or just slowed</param>
    protected void Disrupt(EntityUid target, TimeSpan duration, bool doStun = true)
    {
        _statusEffectsSystem.TryAddStatusEffectDuration(target, BlindnessSystem.BlindingStatusEffect, duration);

        _jittering.DoJitter(target, duration, true);
        if (doStun)
        {
            _stun.TryUpdateParalyzeDuration(target, duration);
        }
        else
        {
            _stun.TryCrawling(target, time: duration);
        }

        _audio.PlayPredicted(new SoundCollectionSpecifier(Sparks), target, null);
        _popup.PopupEntity(Loc.GetString("emp-vulnerable-component-disrupted"), target, target, PopupType.MediumCaution);

    }
}
