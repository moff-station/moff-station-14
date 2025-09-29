using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Antag;
using Robust.Shared.Audio;

namespace Content.Shared._Moffstation.ClockCult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkCultComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "ClockworkFaction";

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier ClockCultStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_wawa.ogg");

    public override bool SessionSpecific => true;
}
