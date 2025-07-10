using Content.Shared._Moffstation.Vampire.Abilities.Components;
using Content.Shared._Moffstation.Vampire.Events;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Drunk;
using Content.Shared.Popups;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Moffstation.Vampire.Abilities.EntitySystems;

/// <summary>
/// This system handles the <see cref="Content.Shared._Moffstation.Vampire.Abilities.Components.AbilityRejuvenateComponent"/>
/// which on initialization here gives the entity the Rejuvenate ability. This also handles that ability's events.
/// </summary>
/// <remarks>
/// todo: Add upgrades
/// </remarks>
public sealed class AbilityRejuvenateSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem  _stamina = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbilityRejuvenateComponent, VampireEventRejuvenateAbility>(OnRejuvenate);
        SubscribeLocalEvent<AbilityRejuvenateComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AbilityRejuvenateComponent> entity, ref MapInitEvent args)
    {
        _action.AddAction(entity.Owner, ref entity.Comp.Action,  entity.Comp.ActionProto, entity.Owner);
    }

    /// <summary>
    /// When the Rejuvenate ability is used this method triggers.
    /// It handles:
    ///     - Removing the KnockedDown and Stunned components.
    ///     - Reducing stamina damage on the entity.
    ///     - Reducing drunkenness on the entity.
    ///     - Reducing stutter time on the entity.
    ///     - Admin Logging
    ///     - Generating a Popup to the user
    ///     - Playing the sound specified in the component.
    /// </summary>
    /// <remarks>
    /// todo: Add upgrades
    /// todo: Add some kind of visual feedback, perhaps a brief animation or a dim red light effect?
    /// </remarks>
    private void OnRejuvenate(Entity<AbilityRejuvenateComponent> entity, ref VampireEventRejuvenateAbility args)
    {
        if (args.Handled)
            return;

        if (!TryComp<AbilityRejuvenateComponent>(entity.Owner, out var rejuvenateComp))
            return;

        RemComp<KnockedDownComponent>(entity);
        RemComp<StunnedComponent>(entity);

        if (TryComp<StaminaComponent>(entity, out var stamina))
        {
            stamina.Critical = false; // Takes us out of stam crit immediately.
            // Notably, we don't get any stamina resistance after this from after stam-crit effects.
            // So it is easy to stam-crit the vampire again.
            _stamina.TakeStaminaDamage(entity, rejuvenateComp.StamHealing, stamina);
        }

        _drunkSystem.TryRemoveDrunkenessTime(entity, rejuvenateComp.StatusEffectReductionTime.TotalSeconds);
        _stuttering.DoRemoveStutterTime(entity, rejuvenateComp.StatusEffectReductionTime.TotalSeconds);

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(entity):user} used Rejuvenate.");
        _popup.PopupEntity(Loc.GetString("vampire-rejuvenate-popup"), entity, entity, PopupType.Medium);
        _audio.PlayPvs(rejuvenateComp.Sound, entity);

        args.Handled = true;
    }
}
