using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Vampire.Events;

public sealed partial class VampireEventFeedAbility : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class VampireEventFeedAbilityDoAfter : SimpleDoAfterEvent;
