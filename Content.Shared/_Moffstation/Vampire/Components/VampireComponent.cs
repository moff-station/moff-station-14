using Content.Shared._Moffstation.Vampire.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Components;

[NetworkedComponent]
[RegisterComponent, Access(typeof(SharedVampireSystem))]
public sealed partial class VampireComponent : Component
{
    /// <summary>
    /// The BloodEssence prototype for the shop
    /// </summary>
    [DataField]
    public ProtoId<CurrencyPrototype> BloodEssenceCurrencyPrototype = "BloodEssence";

    [DataField]
    public EntProtoId ActionVampireShopProto = "ActionVampireShop";

    [DataField]
    public EntityUid? ShopAction;
}
