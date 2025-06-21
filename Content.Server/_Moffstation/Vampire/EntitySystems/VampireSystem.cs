using Content.Shared._Moffstation.Vampire.Abilities.Components;
using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared._Moffstation.Vampire;
using Content.Server.Body.Components;
using Content.Server.Store.Systems;
using Content.Shared.Store.Components;
using Content.Shared.Actions;
using Content.Shared.FixedPoint;

namespace Content.Server._Moffstation.Vampire.EntitySystems;

/// <summary>
/// This system is the central place which manages vampires.
/// It's largely responsible for handing their shop and initializing the basic vampire abilities.
/// </summary>
public sealed class VampireSystem : EntitySystem
{
    [Dependency] private readonly StoreSystem _storeSystem = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StoreComponent, VampireShopEvent>(OnShopOpenAction);
    }

    private void OnMapInit(Entity<VampireComponent> entity, ref MapInitEvent args)
    {
        RemComp<RespiratorComponent>(entity); // Don't need them to breath

        // Give the shop action
        _action.AddAction(entity, ref entity.Comp.ShopAction, entity.Comp.ActionVampireShopProto, entity);

        // These components all add actions on map init.
        EnsureComp<AbilityGlareComponent>(entity);
        EnsureComp<AbilityFeedComponent>(entity);
        EnsureComp<AbilityRejuvenateComponent>(entity);
    }

    /// <summary>
    /// Opens or closes the shop for the user.
    /// </summary>
    private void OnShopOpenAction(Entity<StoreComponent> entity, ref VampireShopEvent args)
    {
        _storeSystem.ToggleUi(entity.Owner, entity.Owner, entity.Comp);
    }

    /// <summary>
    /// Handles depositing blood essence into the shop. This is intended to be used by other systems to deposit.
    /// </summary>
    /// <param name="entity">The entity with the Vampire component to deposit Blood Essence into</param>
    /// <param name="amount">The amount of Blood Essence to deposit</param>
    public void DepositEssence(Entity<VampireComponent> entity, float amount)
    {
        if (amount <= 0.0f)
            return;

        _storeSystem.TryAddCurrency(new Dictionary<string, FixedPoint2>
                { { entity.Comp.BloodEssenceCurrencyPrototype, amount } },
            entity);
    }
}
