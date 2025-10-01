using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.ClockCult.Events;

/// <summary>
/// Action event for attempting to abscond to Reebe.
/// </summary>
public sealed partial class ClockworkAbscondActionEvent : InstantActionEvent;

/// <summary>
/// DoAfterevent used to teleport a cultist to Reebe.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ClockworkAbscondDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// DoAfterevent used to teleport a cultist (and whatever they're pulling) to Reebe.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ClockworkAbscondPullingDoAfterEvent : SimpleDoAfterEvent;
