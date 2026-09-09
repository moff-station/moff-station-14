using Robust.Shared.Audio;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

/// <summary>
/// Plays an announcement a gamerule starts.
/// </summary>
[RegisterComponent]
public sealed partial class MoffAnnounceOnRuleStartedComponent : Component
{
    [DataField(required: true)]
    public LocId Message = string.Empty;

    [DataField]
    public LocId? Sender;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public Color? Color;

    [DataField]
    public bool ServerAnnouncement;
}
