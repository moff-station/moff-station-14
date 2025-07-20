using Content.Shared._Moffstation.Vampire.EntitySystems;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Vampire.Components;

/// <summary>
/// This component is the top of the cascade which makes someone a vampire.
/// Responsible for setting up some of the more intricate details of initializing a vampire,
/// this component and related systems (<see cref="Content.Shared._Moffstation.Vampire.EntitySystems"/> and
/// </summary>
[RegisterComponent]
public sealed partial class VampireComponent : Component
{
    /// <summary>
    /// The BloodEssence prototype for the shop.
    /// </summary>
    [DataField]
    public ProtoId<CurrencyPrototype> BloodEssenceCurrencyPrototype = "BloodEssence";

    /// <summary>
    /// The vampire's shop action prototype.
    /// </summary>
    [DataField]
    public EntProtoId ActionVampireShopProto = "ActionVampireShop";

    /// <summary>
    /// A place for the action to be stored.
    /// </summary>
    [DataField]
    public EntityUid? ShopAction;
}
