using Content.Shared._Funkystation.WashingMachine;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.Stains;

public sealed partial class StainRepellentSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private OpenableSystem _openable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<ItemComponent> _itemQuery;
    [Dependency] private EntityQuery<StainRepellentCoatedComponent> _stainRepellentCoatedQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainRepellentComponent, AfterInteractEvent>(OnInteract, after: [typeof(OpenableSystem)]);
    }

    public bool IsStainRepellent(Entity<StainRepellentCoatedComponent?> entity) =>
        _stainRepellentCoatedQuery.HasComp(entity);

    private void OnInteract(Entity<StainRepellentComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryCoat(entity, target, args.User))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUtilityVerb(Entity<StainRepellentComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess ||
            args.Target is not { Valid: true } target ||
            _openable.IsClosed(entity))
            return;

        var user = args.User;
        var verb = new UtilityVerb
        {
            Act = () => TryCoat(entity, target, user),
            IconEntity = GetNetEntity(entity),
            Text = Loc.GetString("stain-repellent-verb-text"),
            Message = Loc.GetString("stain-repellent-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    private bool TryCoat(Entity<StainRepellentComponent> entity, EntityUid target, EntityUid actor)
    {
        if (!_itemQuery.HasComp(target))
        {
            _popup.PopupEntity(
                Loc.GetString("stain-repellent-not-item", ("target", target)),
                actor,
                actor,
                PopupType.Medium
            );
            return false;
        }

        if (_stainRepellentCoatedQuery.HasComp(target))
        {
            _popup.PopupEntity(
                Loc.GetString("stain-repellent-failure-already-repellent", ("target", target)),
                actor,
                actor,
                PopupType.Medium
            );
            return false;
        }

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var repellentSolution, out _))
        {
            this.AssertOrLogError($"{ToPrettyString(entity)} is missing expected solution {entity.Comp.Solution}");
            return false;
        }

        var quantity = _solutionContainer.RemoveReagent(
            repellentSolution.Value,
            entity.Comp.Reagent,
            entity.Comp.ConsumptionUnit
        );
        if (quantity <= 0)
        {
            _popup.PopupEntity(
                Loc.GetString("stain-repellent-failure-empty", ("target", target)),
                actor,
                actor,
                PopupType.Medium
            );
            return false;
        }

        _audio.PlayPredicted(entity.Comp.Squeeze, entity.Owner, actor);
        _popup.PopupEntity(
            Loc.GetString("stain-repellent-success", ("target", target)),
            actor,
            actor,
            PopupType.Medium
        );
        _adminLogger.Add(
            LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(actor):actor} applied stain-repellent coating to {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}"
        );

        var blocker = EnsureComp<StainRepellentCoatedComponent>(target);
        blocker.RemovalOnWashingChance = entity.Comp.RemovalOnWashingChance;
        Dirty(target, blocker);

        return true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<StainRepellentCoatedComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("stain-repellent-examine-coated"));
    }

    [SubscribeLocalEvent]
    private void OnWashingMachineWashed(
        Entity<StainRepellentCoatedComponent> entity,
        ref WashingMachineWashedEvent args
    )
    {
        if (SharedRandomExtensions.PredictedProb(_timing, entity.Comp.RemovalOnWashingChance, GetNetEntity(entity)))
        {
            RemCompDeferred<StainRepellentCoatedComponent>(entity);
        }
    }
}
