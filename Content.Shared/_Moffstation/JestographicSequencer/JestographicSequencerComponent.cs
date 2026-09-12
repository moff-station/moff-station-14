using Content.Shared.Access.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.JestographicSequencer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JestographicSequencerComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier ReverseSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");
}
