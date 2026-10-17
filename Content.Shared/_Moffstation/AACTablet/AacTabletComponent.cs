using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.AacTablet;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(AacTabletSystem))]
public sealed partial class AacTabletComponent : Component
{
    /// Delay between sending phrases.
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);

    /// The <see cref="AacPhrasePackPrototype"/>s available on this tablet.
    [DataField, AutoNetworkedField]
    public List<ProtoId<AacPhrasePackPrototype>> Packs = new();
}
