using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Antag;
using Robust.Shared.Audio;

namespace Content.Shared._Moffstation.ClockCult.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ClockworkCultComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "ClockworkFaction";

    /// <summary>
    /// The Action for the Abscond spell
    /// </summary>
    [DataField]
    public EntProtoId? ClockworkAbscondAction = "ActionClockworkAbscond";

    /// <summary>
    /// The action entity associated with the Abscond spell
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ClockworkAbscondActionEntity;

    /// <summary>
    /// Time it takes to abscond to Reebe
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AbscondWindup = TimeSpan.FromSeconds(1.5);

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier ClockCultStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_wawa.ogg");

    public override bool SessionSpecific => true;
}
