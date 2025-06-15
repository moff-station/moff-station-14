using Content.Server._Moffstation.Vampire.Abilities.Components;
using Content.Server.Body.Components;
using Content.Shared._Moffstation.Vampire.Components;
using Content.Server.Store.Systems;
using Content.Shared._Moffstation.Vampire;
using Content.Shared._Moffstation.Vampire.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Vampire.EntitySystems;

public sealed partial class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VampireComponent, VampireShopEvent>(OnShopOpenAction);
    }

    private void OnMapInit(EntityUid uid, VampireComponent? comp, MapInitEvent args)
    {
        if (!Resolve(uid, ref comp))
            return;

        _entityManager.RemoveComponent<RespiratorComponent>(uid); // Don't need them to breath

        // give the actions
        _action.AddAction(uid, ref comp.ShopAction, comp.ActionVampireShopProto, uid);

        EnsureComp<AbilityGlareComponent>(uid);
        EnsureComp<AbilityFeedComponent>(uid);
        EnsureComp<AbilityRejuvenateComponent>(uid);
    }

    private void OnShopOpenAction(EntityUid uid, VampireComponent comp, VampireShopEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _storeSystem.ToggleUi(uid, uid, store);
    }

    public void DepositEssence(EntityUid uid, VampireComponent? comp, float amount)
    {
        if (amount <= 0.0f)
            return;

        if (!Resolve(uid, ref comp))
            return;

        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _storeSystem.TryAddCurrency(new Dictionary<string, FixedPoint2>
                { { comp.BloodEssenceCurrencyPrototype, amount } },
                uid);
    }
}
