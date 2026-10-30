using Content.Shared.Access.Components;
using Robust.Shared.Audio;
using Content.Shared.Silicons.Laws;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.JestographicSequencer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JestographicSequencerComponent : Component
{
    /// <summary>
    ///The lawset given to funny cyborgs this is used on.
    /// </summary>
    [DataField]
    public ProtoId<SiliconLawsetPrototype> Lawset = "PranksimovLawset";

    [DataField, AutoNetworkedField]
    public SoundSpecifier ReverseSound = new SoundPathSpecifier(
        "/Audio/Items/bikehorn.ogg",
        AudioParams.Default.AddVolume(-6f)); //Plays when cmag used. Bikehorn was a biiiiit too loud
}
