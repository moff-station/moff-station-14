using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Vampire.Events;

/// <summary>
/// We feeding
/// </summary>
public sealed partial class VampireEventFeedAbility : EntityTargetActionEvent;

/// <summary>
/// We finish feeding
/// </summary>
[Serializable, NetSerializable]
public sealed partial class VampireEventFeedAbilityDoAfter : SimpleDoAfterEvent;
