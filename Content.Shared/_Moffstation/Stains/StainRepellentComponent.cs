using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Stains;

/// This component indicates that an entity has a stain-resistant coating applied.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StainRepellentCoatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float RemovalOnWashingChance = 0f;
}

/// This component indicates that an item is a container of stain-repellent coating and can be applied to stainable
/// clothing.
// TODO This is copied from SpaceGlue/Lube, and it seems like the shared behavior between these three things could be unified
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StainRepellentSystem))]
public sealed partial class StainRepellentComponent : Component
{
    /// <summary>
    /// Noise made when the coating is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    /// <summary>
    /// Solution on the entity that contains the coating reagent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Solution = "drink";

    /// <summary>
    /// Reagent that will be used as the stain-repellent coating.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "StainRepellent";

    /// <summary>
    /// Reagent consumption per use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ConsumptionUnit = FixedPoint2.New(5);

    [DataField, AutoNetworkedField]
    public float RemovalOnWashingChance = .5f;
}
