using Content.Shared._Moffstation.Xenoarchaeology.Artifact.XAT.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Xenoarchaeology.Artifact.XAT.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(XATDiceRollSystem)), AutoGenerateComponentState]
public sealed partial class XATDiceRollComponent : Component
{
    /// <summary>
    /// Required amount of sides for the trigger (null = don't care)
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? RequiredSides;

    /// <summary>
    /// The dice value that will trigger the node.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TriggerValue;

    /// <summary>
    /// Range within which the dice must be rolled for the trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 15;
}
