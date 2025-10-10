using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting;

/// <summary>
/// Generic object meant to hold serializable data for a votable option.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class ESVoteOption
{
    /// <summary>
    /// String used to display the vote option in the voting UI.
    /// </summary>
    [DataField]
    public string DisplayString;
}
