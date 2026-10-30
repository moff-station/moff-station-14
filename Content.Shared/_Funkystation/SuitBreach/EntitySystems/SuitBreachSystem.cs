using Content.Shared._Funkystation.CCVar;
using Content.Shared._Funkystation.SuitBreach.Components;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Gravity;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Funkystation.SuitBreach.EntitySystems;

/// <summary>
/// handles the shared severity for suit breaches
/// </summary>
public abstract partial class SuitBreachSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedToolSystem _tools = default!; // Moff - Use tool quality over tag
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;

    [Dependency] private EntityQuery<SuitBreachableComponent> _breachableQuery; // Moff
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery;

    // qualifying damage types that can puncture a suit
    private static readonly ProtoId<DamageTypePrototype>[] PuncturingDamageTypes =
        ["Piercing", "Slash", "Heat"];

    private static readonly ProtoId<ToolQualityPrototype> SabotageQuality = "Slicing"; // Moff - Use tool quality over tag

    protected bool Enabled { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, SuitBreachCVars.Enabled, v => Enabled = v, true);
    }

    // weightless wearers get pushed by the venting gas
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Enabled)
            return;

        foreach (var ent in EntityQueryEnumerator<SuitBreachedComponent>())
        {
            if (!ent.Comp.IsLeaking
                || !TryGetWearer(ent.Owner, out var wearer)
                || !_gravity.IsWeightless(wearer)
                || !_physicsQuery.TryComp(wearer, out var physics))
                continue;

            var impulse = ent.Comp.LeakImpulsePerSecond.GetValueOrDefault(ent.Comp.Severity, 0f);
            if (impulse <= 0f)
                continue;

            _physics.WakeBody(wearer, body: physics);
            _physics.ApplyLinearImpulse(wearer, ent.Comp.LeakAngle.ToVec() * impulse * frameTime, body: physics);
        }
    }

    /// <summary>
    /// examining a breached suit tells you how bad it is
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSuitExamined(Entity<SuitBreachedComponent> ent, ref ExaminedEvent args)
    {
        var locKey = ent.Comp.Severity switch
        {
            SuitBreachSeverity.Minor => "suit-breach-examine-minor",
            SuitBreachSeverity.Major => "suit-breach-examine-major",
            SuitBreachSeverity.Catastrophic => "suit-breach-examine-catastrophic",
            _ => null,
        };

        if (locKey != null)
            args.PushMarkup(Loc.GetString(locKey));
    }

    /// <summary>
    /// examining a sealant canister shows how many charges it has left
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCanisterExamined(Entity<SuitSealantCanisterComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(ent.Comp.Charges > 0
            ? Loc.GetString("suit-breach-sealant-examine-charges", ("charges", ent.Comp.Charges))
            : Loc.GetString("suit-breach-sealant-examine-empty"));
    }

    // rolls a breach off a hit, escalates severity if it happens
    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<DamageableComponent> ent, ref DamageDealtEvent args)
    {
        if (!Enabled)
            return;

        // Moff start - don't assume outerclothing
        // if (!_inventory.TryGetSlotEntity(uid, "outerClothing", out var suitUid))
        //     return;
        //
        // if (!TryComp<SuitBreachableComponent>(suitUid, out var breachable))
        //     return;
        // Moff end

        var qualifyingDamage = 0f;
        foreach (var damageType in PuncturingDamageTypes)
        {
            if (args.Damage.DamageDict.TryGetValue(damageType, out var amount) && amount > 0)
                qualifyingDamage += (float)amount;
        }

        // Moff start - Don't assume outerclothing
        foreach (var suit in _breachableQuery.ResolveAll(_inventory.GetHandOrInventoryEntities(ent.Owner), logMissing: false))
        {
            if (qualifyingDamage < suit.Comp.MinDamageToRoll)
                continue;

            if (!SharedRandomExtensions.PredictedProb(Timing, suit.Comp.BreachChance, GetNetEntity(suit), GetNetEntity(args.Origin)))
                continue;

            var severity = SeverityFromDamage(suit.Comp, qualifyingDamage); // Moff - Breach thresholds on the component
            TryPuncture((suit.Owner, null), severity, ent.Owner, args.Origin);
        }
        // Moff end
    }

    [SubscribeLocalEvent]
    private void OnInteractUsingSuitedTarget(Entity<InternalsComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Moff start - don't assume outerclothing
        EntityUid? suitUid = null;
        var slots = _inventory.GetSlotEnumerator(target.Owner);
        while (slots.NextItem(out var item))
        {
            if (!_breachableQuery.HasComp(item))
                continue;

            suitUid = item;
            if (HasComp<SuitBreachedComponent>(item))
                break;
        }

        if (suitUid == null)
            return;
        // Moff end

        TryApplySealant(suitUid.Value, target.Owner, ref args);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsingSuitItem(Entity<SuitBreachableComponent> suit, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (Enabled && _tools.HasQuality(args.Used, SabotageQuality)) // Moff - Use tool quality over tag
        {
            if (IsWorn(suit.Owner))
            {
                _popup.PopupEntity(Loc.GetString("suit-breach-sabotage-worn"), suit.Owner, args.User);
                args.Handled = true;
                return;
            }

            if (suit.Comp.SabotageAlwaysBreaches)
            {
                // Moff start - Already-ruined sabotage popup
                if (TryComp<SuitBreachedComponent>(suit, out var breached) && breached.Severity == SuitBreachSeverity.Catastrophic)
                {
                    _popup.PopupEntity(Loc.GetString("suit-breach-sabotage-already-breached"), suit.Owner, args.User);
                    args.Handled = true;
                    return;
                }
                // Moff end

                var doAfterArgs = new DoAfterArgs(EntityManager,
                    args.User,
                    suit.Comp.SabotageDelay,
                    new SuitSabotageDoAfterEvent(),
                    suit.Owner,
                    target: suit.Owner,
                    used: args.Used)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                };

                args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
                return;
            }
        }

        TryApplySealant(suit.Owner, null, ref args);
    }

    [SubscribeLocalEvent]
    private void OnSabotageDoAfter(Entity<SuitBreachableComponent> suit, ref SuitSabotageDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (IsWorn(suit.Owner))
        {
            _popup.PopupEntity(Loc.GetString("suit-breach-sabotage-worn"), suit.Owner, args.User);
            return;
        }

        var tempSuit = new Entity<SuitBreachedComponent?>(suit.Owner, null);
        if (!TryEscalate(ref tempSuit, SuitBreachSeverity.Catastrophic))
            return;

        OnSeverityChanged(suit.Owner);
        _popup.PopupEntity(Loc.GetString("suit-breach-sabotage-success"), suit.Owner, args.User);
    }

    [SubscribeLocalEvent]
    private void OnSealDoAfter(Entity<SuitBreachedComponent> suit, ref SuitSealDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } usedUid)
            return;

        if (!TryComp(usedUid, out SuitSealantCanisterComponent? canister) && !HasComp<SuitPatchComponent>(usedUid))
            return;

        if (canister != null)
        {
            if (canister.Charges <= 0)
                return;

            canister.Charges--;
            Dirty(usedUid, canister);
            Audio.PlayPredicted(canister.ApplySound, suit.Owner, args.User);
        }
        else if (TryComp(usedUid, out StackComponent? stackComp))
        {
            if (!_stack.TryUse((usedUid, stackComp), 1))
                return;
        }
        else
        {
            PredictedQueueDel(usedUid);
        }

        args.Handled = true;

        var newSeverity = Regress(suit);
        OnSeverityChanged(suit.Owner);

        var message = newSeverity == SuitBreachSeverity.None
            ? Loc.GetString("suit-breach-seal-fully-sealed")
            : Loc.GetString("suit-breach-seal-partially-sealed");
        _popup.PopupEntity(message, suit.Owner, args.User, PopupType.Medium);
    }

    private void TryApplySealant(EntityUid suitUid, EntityUid? wearer, ref InteractUsingEvent args)
    {
        if (!TryComp<SuitSealantCanisterComponent>(args.Used, out var canister) & !TryComp<SuitPatchComponent>(args.Used, out var patch))
            return;

        if (wearer == null && TryGetWearer(suitUid, out var actualWearer))
            wearer = actualWearer;

        if (!TryComp<SuitBreachedComponent>(suitUid, out _))
        {
            var message = wearer switch
            {
                null => Loc.GetString("suit-breach-seal-nothing-to-fix-item"),
                var w when w == args.User => Loc.GetString("suit-breach-seal-nothing-to-fix-self"),
                _ => Loc.GetString("suit-breach-seal-nothing-to-fix-other", ("target", Identity.Name(wearer.Value, EntityManager, args.User))),
            };
            _popup.PopupEntity(message, suitUid, args.User);
            args.Handled = true;
            return;
        }

        if (canister is { Charges: <= 0 })
        {
            _popup.PopupEntity(Loc.GetString("suit-breach-seal-canister-empty"), suitUid, args.User);
            args.Handled = true;
            return;
        }

        var isSelf = wearer != null && wearer == args.User;
        TimeSpan delay;
        bool breakOnMove;

        if (canister != null)
        {
            delay = isSelf ? canister.SelfApplyDelay : canister.OtherApplyDelay;
            breakOnMove = false;
        }
        else if (patch != null)
        {
            delay = isSelf ? patch.SelfApplyDelay : patch.OtherApplyDelay;
            breakOnMove = patch.BreakOnMove;
        }
        else
        {
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            delay,
            new SuitSealDoAfterEvent(),
            suitUid,
            target: wearer ?? suitUid,
            used: args.Used)
        {
            BreakOnMove = breakOnMove,
            BreakOnDamage = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void TryPuncture(Entity<SuitBreachedComponent?> suit, SuitBreachSeverity severity, EntityUid victim, EntityUid? attacker = null)
    {
        if (!TryEscalate(ref suit, severity, attacker))
            return;

        OnSeverityChanged(suit.Owner);
        _popup.PopupEntity(Loc.GetString("suit-breach-warning"), victim, victim, PopupType.LargeCaution);
    }

    // Moff start - Breach thresholds on the component
    private static SuitBreachSeverity SeverityFromDamage(SuitBreachableComponent breachable, float damage)
    {
        if (damage >= breachable.CatastrophicDamageThreshold)
            return SuitBreachSeverity.Catastrophic;

        if (damage >= breachable.MajorDamageThreshold)
            return SuitBreachSeverity.Major;

        return SuitBreachSeverity.Minor;
    }
    // Moff end

    protected bool TryGetWearer(EntityUid suitUid, out EntityUid wearer)
    {
        wearer = Transform(suitUid).ParentUid;
        return _inventory.TryGetContainingSlot(suitUid, out _); // Moff - dont assume outerclothing
    }

    private bool IsWorn(EntityUid suitUid)
    {
        return TryGetWearer(suitUid, out _);
    }

    /// <summary>
    /// called after a suit's breach severity goes up or down
    /// </summary>
    protected virtual void OnSeverityChanged(EntityUid suit)
    {
    }

    protected void SetLeaking(Entity<SuitBreachedComponent> suit, bool leaking)
    {
        if (suit.Comp.IsLeaking == leaking)
            return;

        suit.Comp.IsLeaking = leaking;
        Dirty(suit);
    }

    private Angle LeakAngleFrom(EntityUid suit, EntityUid? attacker)
    {
        if (attacker is { } attackerUid && Exists(attackerUid))
        {
            var suitPos = TransformSystem.GetMapCoordinates(suit);
            var attackerPos = TransformSystem.GetMapCoordinates(attackerUid);
            var away = suitPos.Position - attackerPos.Position;
            if (suitPos.MapId == attackerPos.MapId && away.LengthSquared() > 0.01f)
                return new Angle(away);
        }

        var random = SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(suit));
        random.NextFloat();
        return random.NextAngle();
    }

    /// <summary>
    /// escalates breach severity
    /// </summary>
    protected bool TryEscalate(ref Entity<SuitBreachedComponent?> suit, SuitBreachSeverity incoming, EntityUid? attacker = null)
    {
        if (incoming == SuitBreachSeverity.None)
            return false;

        if (!Resolve(suit, ref suit.Comp, logMissing: false))
        {
            var comp = EnsureComp<SuitBreachedComponent>(suit);
            comp.Severity = incoming;
            comp.LeakAngle = LeakAngleFrom(suit.Owner, attacker);
            Dirty(suit, comp);
            suit.Comp = comp;
            return true;
        }

        if (suit.Comp.Severity == SuitBreachSeverity.Catastrophic)
            return false;

        // if the new breach isn't strictly worse than current, make it worse anyway so that many small hits still accumulate
        var nextSeverity = incoming > suit.Comp.Severity
            ? incoming
            : suit.Comp.Severity + 1;

        suit.Comp.Severity = nextSeverity;
        Dirty(suit, suit.Comp);
        return true;
    }

    /// <summary>
    /// regresses breach severity by one tier
    /// </summary>
    protected SuitBreachSeverity Regress(Entity<SuitBreachedComponent> suit)
    {
        var next = suit.Comp.Severity switch
        {
            SuitBreachSeverity.Catastrophic => SuitBreachSeverity.Major,
            SuitBreachSeverity.Major => SuitBreachSeverity.Minor,
            _ => SuitBreachSeverity.None,
        };

        if (next == SuitBreachSeverity.None)
        {
            RemComp<SuitBreachedComponent>(suit);
            return SuitBreachSeverity.None;
        }

        suit.Comp.Severity = next;
        Dirty(suit, suit.Comp);
        return next;
    }
}
