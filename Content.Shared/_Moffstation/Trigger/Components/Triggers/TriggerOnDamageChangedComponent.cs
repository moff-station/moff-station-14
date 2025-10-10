using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Trigger.Components.Triggers;

/// <summary>
/// This <see cref="BaseTriggerOnXComponent">trigger component</see> triggers when its owner receives a
/// <see cref="DamageChangedEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnDamageChangedComponent : BaseTriggerOnXComponent;

/// <summary>
/// This <see cref="BaseTriggerOnXComponent">trigger component</see> triggers when the entity which has its owner
/// equipped receives a <see cref="DamageChangedEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnEquipeeDamageChangedComponent : BaseTriggerOnXComponent;

/// <summary>
/// This <see cref="BaseTriggerOnXComponent">trigger component</see> triggers when the entity which is holding its owner
/// in-hand receives a <see cref="DamageChangedEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnHolderDamageChangedComponent : BaseTriggerOnXComponent;
