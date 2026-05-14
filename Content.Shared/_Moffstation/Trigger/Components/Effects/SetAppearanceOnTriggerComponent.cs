using Content.Shared._Moffstation.Trigger.Systems;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Trigger.Components.Effects;

/// A <see cref="BaseXOnTriggerComponent"/> which sets <see cref="AppearanceComponent">appearance data</see> as
/// specified in <see cref="Data"/>. This appearance data remains for <see cref="Duration"/> and then is reset to its
/// previous state using <see cref="SetAppearanceOnTriggerRestorationComponent"/>.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SetAppearanceOnTriggerSystem))]
public sealed partial class SetAppearanceOnTriggerComponent : BaseXOnTriggerComponent
{
    /// Appearance data to set when the entity is triggered.
    [DataField(required: true)]
    public Dictionary<Enum, string> Data = new();

    /// How long the triggered appearance data is set for. If null, it remains indefinitely.
    [DataField]
    public TimeSpan? Duration = null;
}

/// A component which stores <see cref="AppearanceComponent">appearance data</see> to be applied
/// <see cref="RestoreAt">at some point in the future</see>.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SetAppearanceOnTriggerSystem))]
public sealed partial class SetAppearanceOnTriggerRestorationComponent : BaseXOnTriggerComponent
{
    /// The appearance data state to restore.
    [DataField]
    public List<(Enum, object?)> RestoreData = new();

    /// When <see cref="RestoreData"/> should be applied.
    [DataField(required: true), AutoPausedField]
    public TimeSpan RestoreAt;
}
